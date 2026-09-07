using FinanceCalculator;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.ViewModels;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using MySqlX.XDevAPI.Common;
using Org.BouncyCastle.Utilities.Zlib;
using System.Data.Common;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MyFinances.Controllers
{
    public class TransactionHelper(AppDBContext appDBContext, ContractFull cf, IConfiguration configuration)
    {
        private readonly AppDBContext _dBContext = appDBContext;
        private readonly IConfiguration _configuration = configuration;
        private readonly ContractFull _contractFull = cf;

        public int EMR { get; private set; } = 0;

        public async Task<List<PlanTransaction>> OverdueTransactions(DateTime referenceDate)
        {
            return await _dBContext.PlanTransactions.AsQueryable().Where(m=>m.TransactionDate <= referenceDate && !m.ExecutedTransaction).AsNoTracking().ToListAsync();
        }

        public async Task<string> DeleteAllContractTransactions()
        {
            string result = string.Empty;
            if (_contractFull != null)
            {
                List<FinTransaction> finTransactions = await _dBContext.FinTransactions.Where(m=>m.ContractIdentifier == _contractFull.FinContract.IdFinAccount).ToListAsync();
                if (finTransactions.Count > 0)
                {
                    foreach (FinTransaction transaction in finTransactions)
                    {
                        _dBContext.FinTransactions.Attach(transaction).State = EntityState.Deleted;
                    }
                }
                List<PlanTransaction> planTransactions = await _dBContext.PlanTransactions.Where(m => m.ContractIdentifier == _contractFull.FinContract.IdFinAccount).ToListAsync();
                if(planTransactions.Count > 0)
                {
                    foreach (var item in planTransactions)
                    {
                        _dBContext.PlanTransactions.Attach(item).State = EntityState.Deleted;
                    }
                }
                try
                {
                    await _dBContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    result = $"Error in deleting all transactios for a contract" + ex.Message + "\n" + ex.Source;
                }
            }
            return result;
        }

        public async Task<string> DeleteAllContractPlanTransactions()
        {
            string result = string.Empty;
            if (_contractFull != null)
            {
                List<PlanTransaction> planTransactions = await _dBContext.PlanTransactions.Where(m => m.ContractIdentifier == _contractFull.FinContract.IdFinAccount).ToListAsync();
                if (planTransactions.Count > 0)
                {
                    foreach (var item in planTransactions)
                    {
                        _dBContext.PlanTransactions.Attach(item).State = EntityState.Deleted;
                    }
                }
            }
            try
            {

            }
            catch (Exception ex)
            {
                result = $"Error deleting all contract plan transactions \n {ex.Message}";
            }
            return result;
        }

        public async Task AddPlanTransactions(DateTime startDate, decimal purchaseBal = 0, decimal casBal = 0, decimal limitCardPayOff = 0)
        {
            //get the enddate for plantransactions
            CalculatorDates cada = new(_contractFull.FinContract.IdFrequency);
            DateTime firstInvoice = cada.NextCycleDate(_contractFull.FinContract.InvoiceDate, startDate);
            GeneralHelper gh = new(_dBContext, _configuration);
            DateTime endDatePT = gh.EndPeriodPlanTransaction(firstInvoice, _contractFull.FinContract.EndDate, _contractFull.FinContract.IdEndAction);
            //get user tax account id
            int idtaxacc = 0;
            if (_contractFull.FinContract.ContractType == 4 || _contractFull.FinContract.ContractType == 5)
            {
                idtaxacc = await _dBContext.UserContracts.Where(p=>p.IdUser == _contractFull.FinContract.IdContractor).Select(s=>s.UserTaxAccount).FirstOrDefaultAsync();
            }
            int tnt = cada.NumberCyclesPerPeriod(firstInvoice, endDatePT);
            DateTime startThisFY = gh.StartFinancialYear(startDate);
            int ua = await _dBContext.UserContracts.Where(p=>p.IdUser == _contractFull.FinContract.IdContractor).Select(s=>s.UserAccountID).FirstOrDefaultAsync();
            switch (_contractFull.FinContract.ContractType)
            {
                case 1:
                    // NOTE: ensure PurchaseBalance is after the latest payment before the date of execution - DateTime.Today
                    AddDepositPlanTransaction(firstInvoice, tnt, purchaseBal, startThisFY, ua);
                    break;
                case 2:
                    // NOTE: ensure PurchaseBalance is after the latest payment before the date of execution - DateTime.Today
                    int tlr = cada.NumberCyclesPerPeriod(_contractFull.FinContract.StartDate, _contractFull.FinContract.EndDate);
                    AddLoanPlanTransaction(firstInvoice, tnt, purchaseBal, tlr, startThisFY, ua);
                    break;
                case 3:
                    // NOTE: ensure PurchaseBalance is after the latest payment before the date of execution - DateTime.Today
                    AddCreditCardPlanTransaction(firstInvoice, tnt, purchaseBal, casBal, limitCardPayOff, startThisFY, ua);
                    break;
                case 4:
                    await AddEmploymentPlanTs(firstInvoice, tnt, idtaxacc);
                    break;
                case 5:
                    await AddIncomePlanTransaction(firstInvoice, tnt, idtaxacc);
                    break;
                case 6:
                    AddBillPlanTransaction(firstInvoice, tnt);
                    break;
                case 7:
                    AddBillPlanTransaction(firstInvoice, tnt);
                    break;
                default:
                    break;
            }
            return;
        }

        public async Task<string> DeletePlanTransactions(DateTime startDate)
        {
            string result = string.Empty;
            if (_contractFull != null)
            {
                List<PlanTransaction> planTransactions = await _dBContext.PlanTransactions.Where(m => m.ContractIdentifier == _contractFull.FinContract.IdFinAccount && m.TransactionDate >= startDate).ToListAsync();
                if (planTransactions.Count > 0)
                {
                    foreach (var item in planTransactions)
                    {
                        _dBContext.PlanTransactions.Attach(item).State = EntityState.Deleted;
                    }
                    try
                    {
                        await _dBContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        result = ex.Message + "\n" + ex.Source;
                    }
                }
            }
            return result;
        }

        private void AddDepositPlanTransaction(DateTime firstInvoice, int numberPT, decimal lastBalance, DateTime startFY, int userAccount)
        {
            CalculatorFinance cf = new(_contractFull.Deposit.InterestRatePerYear * 0.01m, _contractFull.FinContract.IdFrequency)
            {
                InitialBalance = lastBalance
            };
            CalculatorDates cda = new(_contractFull.FinContract.IdFrequency);
            IConfigurationSection section = _configuration.GetSection("TransactionDetails");
            var detailRate = section.GetValue<string>("PayIn") ?? "Pay from ";
            var detailInterest = section.GetValue<string>("InterestIn") ?? "Interest from ";
            var detailedPayee = section.GetValue<string>("PayOut") ?? "Payment to ";
            var detailClose = section.GetValue<string>("Close") ?? "Close account ";
            var detailOpen = section.GetValue<string>("Open") ?? "Open account ";
            DateTime td = firstInvoice;
            decimal lb = lastBalance;
            DateTime endFY = startFY.AddYears(1);
            FrequencyConversion fc = new(_contractFull.FinContract.IdFrequency);
            EMR = fc.CycletoMonthConversion(_contractFull.Deposit.DepositRate) * -1;
            for (int i = 0; i < numberPT; i++)
            {
                // deposit rate payment
                PlanTransaction rate = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.FinContract.IdFinAccount,
                    CreditAccount = _contractFull.FinContract.IdPayAccount,
                    TransactionValue = _contractFull.Deposit.DepositRate,
                    TransactionDetail = detailRate,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdFinAccount
                };
                _dBContext.PlanTransactions.Add(rate);
                // payment from the payee
                PlanTransaction payRate = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.FinContract.IdFinAccount,
                    CreditAccount = _contractFull.FinContract.IdPayAccount,
                    TransactionValue = _contractFull.Deposit.DepositRate,
                    TransactionDetail = detailedPayee,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdPayAccount
                };
                _dBContext.PlanTransactions.Add(payRate);
                // interest rate
                decimal interestValue = cf.InterestValuePerCycle(lb);
                PlanTransaction interest = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.FinContract.IdFinAccount,
                    CreditAccount = _contractFull.Deposit.InterestAccount,
                    TransactionValue = interestValue,
                    TransactionDetail = detailInterest,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdFinAccount
                };
                _dBContext.PlanTransactions.Add(interest);
                // balance
                lb += (_contractFull.Deposit.DepositRate + interestValue);
                td = cda.IncrementCycleDate(td);
                if(td > endFY) //next plan transactions will be in the new FY
                {
                    //Add ending FY closure transaction
                    PlanTransaction closeFY = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(endFY.AddDays(-1)),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = endFY.AddDays(-1),
                        DebitAccount = userAccount,
                        CreditAccount = _contractFull.FinContract.IdFinAccount,
                        TransactionValue = lb,
                        TransactionDetail = detailClose,
                        ExecutedTransaction = false,
                        IdSource = userAccount
                    };
                    _dBContext.PlanTransactions.Add(closeFY);
                    //Add starting FY opening transaction
                    PlanTransaction openFY = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(endFY),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = endFY,
                        DebitAccount = _contractFull.FinContract.IdFinAccount,
                        CreditAccount = userAccount,
                        TransactionValue = lb,
                        TransactionDetail = detailOpen,
                        ExecutedTransaction = false,
                        IdSource = userAccount
                    };
                    _dBContext.PlanTransactions.Add(openFY);
                    //increment FY
                    startFY = endFY;
                    endFY = startFY.AddYears(1);
                }
            }
            return ;
        }

        private void AddLoanPlanTransaction(DateTime firstInvoice, int numberPT, decimal lastBalance, int totalRates, DateTime startFY, int userAccount)
        {
            if (lastBalance <= 0 || totalRates<=0 || numberPT <=0)
            {
                return;
            }
            CalculatorFinance cf = new(_contractFull.Loan.LoanInterest * 0.01m, _contractFull.FinContract.IdFrequency)
            {
                InitialBalance = _contractFull.Loan.LoanAmount,
                TotalPayments = totalRates,
                ExtraPay = _contractFull.Loan.ExtraPay
            };
            CalculatorDates cda = new(_contractFull.FinContract.IdFrequency);
            IConfigurationSection section = _configuration.GetSection("TransactionDetail");
            var detailRate = section.GetValue<string>("PayOut") ?? "Payment to ";
            var detailInterest = section.GetValue<string>("InterestOut") ?? "Interest to ";
            var detailReceived = section.GetValue<string>("PayIn") ?? "Pay from ";
            var detailClose = section.GetValue<string>("Close") ?? "Close account ";
            var detailOpen = section.GetValue<string>("Open") ?? "Open account ";
            DateTime td = firstInvoice;
            decimal[] payrates = new decimal[numberPT];
            decimal[] interestPaid = new decimal[numberPT];
            decimal[] templb = new decimal[numberPT];
            if (_contractFull.Loan.IdLoanType)
            {
                //variable loan
                for(int k = 0; k < numberPT;k++)
                {
                    payrates[k] = cf.LoanVarRateOnBalance(lastBalance);
                    interestPaid[k] = cf.InterestValuePerCycle(lastBalance);
                    lastBalance = lastBalance - payrates[k] + interestPaid[k];
                    templb[k] = lastBalance;
                }
            }
            else
            {
                //fixed loan
                decimal fr = cf.LoanFixedRate();
                for (int k = 0; k < numberPT; k++)
                {
                    payrates[k] = fr;
                    interestPaid[k] = cf.InterestValuePerCycle(lastBalance);
                    lastBalance = lastBalance - payrates[k] + interestPaid[k];
                    templb[k] = lastBalance;
                }
            }
            FrequencyConversion fc = new(_contractFull.FinContract.IdFrequency);
            EMR = -1 * fc.CycletoMonthConversion(payrates[0]);
            DateTime endFY = startFY.AddYears(1);
            for (int i = 0; i < numberPT; i++)
            {
                // payment rate from the payment account
                PlanTransaction rate = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.FinContract.IdFinAccount,
                    CreditAccount = _contractFull.FinContract.IdPayAccount,
                    TransactionValue = payrates[i],
                    TransactionDetail = detailRate,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdPayAccount
                };
                _dBContext.PlanTransactions.Add(rate);
                // payment rate received in the loan account
                PlanTransaction rateReceived = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.FinContract.IdFinAccount,
                    CreditAccount = _contractFull.FinContract.IdPayAccount,
                    TransactionValue = payrates[i],
                    TransactionDetail = detailReceived,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdFinAccount
                };
                _dBContext.PlanTransactions.Add(rateReceived);
                // interest rate
                PlanTransaction interest = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.Loan.IdAccountInterest,
                    CreditAccount = _contractFull.FinContract.IdFinAccount,
                    TransactionValue = interestPaid[i],
                    TransactionDetail = detailInterest,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdFinAccount
                };
                _dBContext.PlanTransactions.Add(interest);
                // balance
                td = cda.IncrementCycleDate(td);
                if (td > endFY) //next plan transactions will be in the new FY
                {
                    //Add ending FY closure transaction
                    PlanTransaction closeFY = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(endFY.AddDays(-1)),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = endFY.AddDays(-1),
                        DebitAccount = _contractFull.FinContract.IdFinAccount,
                        CreditAccount = userAccount,
                        TransactionValue = templb[i],
                        TransactionDetail = detailClose,
                        ExecutedTransaction = false,
                        IdSource = userAccount
                    };
                    _dBContext.PlanTransactions.Add(closeFY);
                    //Add starting FY opening transaction
                    PlanTransaction openFY = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(endFY),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = endFY,
                        DebitAccount = userAccount,
                        CreditAccount = _contractFull.FinContract.IdFinAccount,
                        TransactionValue = templb[i],
                        TransactionDetail = detailOpen,
                        ExecutedTransaction = false,
                        IdSource = userAccount
                    };
                    _dBContext.PlanTransactions.Add(openFY);
                    //increment FY
                    startFY = endFY;
                    endFY = startFY.AddYears(1);
                }
            }
            //reoncile accounts if Payment account != Bank account
            if(_contractFull.FinContract.IdPayAccount != _contractFull.FinContract.IdBankAccount)
            {
                for (int i = 0; i < numberPT; i++)
                {
                    // rate cover
                    PlanTransaction recon = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = td,
                        DebitAccount = _contractFull.FinContract.IdPayAccount,
                        CreditAccount = _contractFull.FinContract.IdBankAccount,
                        TransactionValue = payrates[i],
                        TransactionDetail = detailRate,
                        ExecutedTransaction = false,
                        IdSource = _contractFull.FinContract.IdBankAccount
                    };
                    _dBContext.PlanTransactions.Add(recon);
                    // cover receive
                    PlanTransaction reconPay = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = td,
                        DebitAccount = _contractFull.FinContract.IdPayAccount,
                        CreditAccount = _contractFull.FinContract.IdBankAccount,
                        TransactionValue = payrates[i],
                        TransactionDetail = detailReceived,
                        ExecutedTransaction = false,
                        IdSource = _contractFull.FinContract.IdPayAccount
                    };
                    _dBContext.PlanTransactions.Add(reconPay);
                    td = cda.IncrementCycleDate(td);
                }
            }

            return;
        }

        private async Task AddEmploymentPlanTs(DateTime firstInvoice, int numberPT, int userTaxAcc)
        {
            IConfigurationSection section = _configuration.GetSection("TransactionDetails");
            string detailRate = section.GetValue<string>("PayIn") ?? "Pay from ";
            string detailTax = section.GetValue<string>("TaxOut") ?? "Tax pay to ";
            string detailSuper = section.GetValue<string>("PayOut") ?? "Payment to ";
            DateTime td = firstInvoice;
            CalculatorDates cda = new(_contractFull.FinContract.IdFrequency);
            FrequencyConversion fc = new(_contractFull.FinContract.IdFrequency);
            for (int i = 0; i < numberPT; i++)
            {
                // deposit rate
                PlanTransaction rate = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.FinContract.IdPayAccount,
                    CreditAccount = _contractFull.FinContract.IdFinAccount,
                    TransactionValue = _contractFull.Employment.GrossPaidCycle - _contractFull.Employment.TaxPaidCycle,
                    TransactionDetail = detailRate,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdPayAccount
                };
                _dBContext.PlanTransactions.Add(rate);
                td = cda.IncrementCycleDate(td);
            }
            td = firstInvoice;
            if (_contractFull.Employment.TaxPaidCycle > 0)
            {
                for (int i = 0; i < numberPT; i++)
                {
                    // deposit rate
                    PlanTransaction tax = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = td,
                        DebitAccount = userTaxAcc,
                        CreditAccount = _contractFull.FinContract.IdFinAccount,
                        TransactionValue = _contractFull.Employment.TaxPaidCycle,
                        TransactionDetail = detailTax,
                        ExecutedTransaction = false,
                        IdSource = _contractFull.FinContract.IdFinAccount
                    };
                    _dBContext.PlanTransactions.Add(tax);
                    td = cda.IncrementCycleDate(td);
                }
                // update EMR for the taxation contract
                var etx = await _dBContext.FinContracts.Where(p=>p.IdFinAccount == userTaxAcc).FirstOrDefaultAsync();
                if (etx != null)
                {
                    int etpc = fc.CycletoMonthConversion(_contractFull.Employment.TaxPaidCycle);
                    etx.EquivMonthlyRate += etpc;
                    _dBContext.FinContracts.Update(etx);
                }
            }
            td = firstInvoice;
            if (_contractFull.Employment.SuperPaidCycle > 0)
            {
                for (int i = 0; i < numberPT; i++)
                {
                    // deposit rate
                    PlanTransaction super = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = td,
                        DebitAccount = _contractFull.Employment.IdSuperAccount,
                        CreditAccount = _contractFull.FinContract.IdFinAccount,
                        TransactionValue = _contractFull.Employment.SuperPaidCycle,
                        TransactionDetail = detailSuper,
                        ExecutedTransaction = false,
                        IdSource = _contractFull.FinContract.IdFinAccount
                    };
                    _dBContext.PlanTransactions.Add(super);
                    PlanTransaction srec = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                        ContractIdentifier = _contractFull.Employment.IdSuperAccount,
                        TransactionDate = td,
                        DebitAccount = _contractFull.Employment.IdSuperAccount,
                        CreditAccount = _contractFull.FinContract.IdFinAccount,
                        TransactionValue = _contractFull.Employment.SuperPaidCycle,
                        TransactionDetail = detailRate,
                        ExecutedTransaction = false,
                        IdSource = _contractFull.Employment.IdSuperAccount
                    };
                    _dBContext.PlanTransactions.Add(srec);
                    td = cda.IncrementCycleDate(td);
                }
                // update EMR for the superannuation contract
                var saa = await _dBContext.FinContracts.Where(p=>p.IdFinAccount == _contractFull.Employment.IdSuperAccount).FirstOrDefaultAsync();
                if(saa != null )
                {
                    int semr = fc.CycletoMonthConversion(_contractFull.Employment.SuperPaidCycle);
                    saa.EquivMonthlyRate -= semr;
                    _dBContext.FinContracts.Update(saa);
                }
            }
            EMR = fc.CycletoMonthConversion(_contractFull.Employment.GrossPaidCycle + _contractFull.Employment.SuperPaidCycle);
            return;
        }

        private async Task AddIncomePlanTransaction(DateTime firstInvoice, int numberPT, int userTaxAcc)
        {
            CalculatorDates cda = new(_contractFull.FinContract.IdFrequency);
            IConfigurationSection section = _configuration.GetSection("TransactionDetails");
            string detailRate = section.GetValue<string>("PayIn") ?? "Pay from ";
            string detailTax = section.GetValue<string>("TaxOut") ?? "Tax pay to ";
            DateTime td = firstInvoice;
            for (int i = 0; i < numberPT; i++)
            {
                // deposit rate
                PlanTransaction rate = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.FinContract.IdPayAccount,
                    CreditAccount = _contractFull.FinContract.IdFinAccount,
                    TransactionValue = _contractFull.Income.GrossPerCycle - _contractFull.Employment.TaxPaidCycle,
                    TransactionDetail = detailRate,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdPayAccount
                };
                _dBContext.PlanTransactions.Add(rate);
                td = cda.IncrementCycleDate(td);
            }
            td = firstInvoice;
            FrequencyConversion fc = new(_contractFull.FinContract.IdFrequency);
            if(_contractFull.Income.TaxPaidCycle > 0)
            {
                for (int i = 0; i < numberPT; i++)
                {
                    // deposit rate
                    PlanTransaction tax = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = td,
                        DebitAccount = userTaxAcc,
                        CreditAccount = _contractFull.FinContract.IdFinAccount,
                        TransactionValue = _contractFull.Income.TaxPaidCycle,
                        TransactionDetail = detailTax,
                        ExecutedTransaction = false,
                        IdSource = _contractFull.FinContract.IdFinAccount
                    };
                    _dBContext.PlanTransactions.Add(tax);
                    td = cda.IncrementCycleDate(td);
                }
                // update EMR for the taxation contract
                var etx = await _dBContext.FinContracts.Where(p => p.IdFinAccount == userTaxAcc).FirstOrDefaultAsync();
                if (etx != null)
                {
                    int etpc = fc.CycletoMonthConversion(_contractFull.Employment.TaxPaidCycle);
                    etx.EquivMonthlyRate -= etpc;
                    _dBContext.FinContracts.Update(etx);
                }
            }
            EMR = fc.CycletoMonthConversion(_contractFull.Income.GrossPerCycle);
            return;
        }

        private void AddBillPlanTransaction(DateTime firstInvoice, int numberPT)
        {
            CalculatorDates cda = new(_contractFull.FinContract.IdFrequency);
            IConfigurationSection section = _configuration.GetSection("TransactionDetails");
            string cc = _configuration.GetSection("AccountCategoriesSettings").GetValue<string>("CardCategory")??"21";
            string detailRate = section.GetValue<string>("PayOut") ?? "Payment to ";
            string detailCard = section.GetValue<string>("PayIn") ?? "Pay from ";
            DateTime td = firstInvoice;
            for (int i = 0; i < numberPT; i++)
            {
                // bill rate
                PlanTransaction rate = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.FinContract.IdFinAccount,
                    CreditAccount = _contractFull.FinContract.IdPayAccount,
                    TransactionValue = _contractFull.Bill.BillCycleRate,
                    TransactionDetail = detailRate,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdPayAccount
                };
                _dBContext.PlanTransactions.Add(rate);
                td = cda.IncrementCycleDate(td);
            }
            td = firstInvoice;
            if (_contractFull.FinContract.IdPayAccount.ToString().StartsWith(cc))
            {
                for (int i = 0; i < numberPT; i++)
                {
                    // bank payment rate
                    PlanTransaction cp = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = td,
                        DebitAccount = _contractFull.FinContract.IdPayAccount,
                        CreditAccount = _contractFull.FinContract.IdBankAccount,
                        TransactionValue = _contractFull.Bill.BillCycleRate,
                        TransactionDetail = detailRate,
                        ExecutedTransaction = false,
                        IdSource = _contractFull.FinContract.IdBankAccount
                    };
                    _dBContext.PlanTransactions.Add(cp);
                    // payment account receiving rate
                    PlanTransaction pfb = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = td,
                        DebitAccount = _contractFull.FinContract.IdPayAccount,
                        CreditAccount = _contractFull.FinContract.IdBankAccount,
                        TransactionValue = _contractFull.Bill.BillCycleRate,
                        TransactionDetail = detailCard,
                        ExecutedTransaction = false,
                        IdSource = _contractFull.FinContract.IdPayAccount
                    };
                    _dBContext.PlanTransactions.Add(pfb);
                    td = cda.IncrementCycleDate(td);
                }
            }
            FrequencyConversion fc = new(_contractFull.FinContract.IdFrequency);
            EMR = fc.CycletoMonthConversion(_contractFull.Bill.BillCycleRate);
            return;
        }

        private void AddCreditCardPlanTransaction(DateTime firstInvoice, int numberPT, decimal purchaseBal, decimal cashBalance, decimal payOff, DateTime startFY, int userAccount)
        {
            if (purchaseBal <= 0 || numberPT <= 0) return;
            CalculatorFinance cf = new(_contractFull.CreditCard.CreditCardInterest * 0.01m, _contractFull.FinContract.IdFrequency)
            {
                InitialBalance = purchaseBal,
                ExtraPay = _contractFull.CreditCard.ExtraPay,
                TotalPayments = numberPT
            };
            CalculatorDates cda = new(_contractFull.FinContract.IdFrequency);
            IConfigurationSection section = _configuration.GetSection("TransactionDetails");
            var detailRate = section.GetValue<string>("Rate") ?? "Payment to ";
            var detailInterest = section.GetValue<string>("InterestOut") ?? "Interest to ";
            var detailFee = section.GetValue<string>("FeeOut") ?? "Fee to ";
            var detailReceived = section.GetValue<string>("PayIn") ?? "Pay from ";
            var detailClose = section.GetValue<string>("Close") ?? "Close account ";
            var detailOpen = section.GetValue<string>("Open") ?? "Open account ";
            DateTime td = firstInvoice;
            decimal clBal = purchaseBal;
            decimal clCash = cashBalance;
            decimal[] purchaseRate = new decimal[numberPT];
            decimal[] interestPurch = new decimal[numberPT];
            decimal[] interestCash = new decimal[numberPT];
            decimal[] cashRate = new decimal[numberPT];
            decimal[] balance = new decimal[numberPT];
            decimal[] insurancePaid = new decimal[numberPT];
            bool hasInsurance = _contractFull.CreditCard.CreditCardInsuranceRate > 0;
            for (int i = 0; i<cf.TotalPayments; i++)
            {
                purchaseRate[i] = cf.CardRateOnBalance(clBal,(decimal)_contractFull.CreditCard.CreditCardPayPercent, payOff );
                interestPurch[i] = cf.InterestValuePerCycle(clBal);
                cashRate[i] = cf.CardRateOnBalance(clCash, (decimal)_contractFull.CreditCard.CreditCardPayPercent, payOff, _contractFull.CreditCard.CashRate);
                interestCash[i] = cf.InterestValuePerCycle(clCash, _contractFull.CreditCard.CashRate);
                if(_contractFull.CreditCard.ExtraPay > 0)
                {
                    if(cashRate[i] > 0) { cashRate[i] += _contractFull.CreditCard.ExtraPay; }
                    else { purchaseRate[i] += _contractFull.CreditCard.ExtraPay; }
                }
                clBal += (interestPurch[i] - purchaseRate[i]);
                clCash += (interestCash[i] - cashRate[i]);
                if(clCash < 0) { clBal += clCash; clCash = 0; } // in the event that the ExtraPay brings the balance to negative, transfer the funds to purchses
                balance[i] = clBal + clCash;
                if (hasInsurance)
                {
                    insurancePaid[i] = Decimal.Multiply((clBal + clCash), _contractFull.CreditCard.CreditCardInsuranceRate * 0.01m);
                }
                else
                {
                    insurancePaid[i] = 0;
                }
            }
            FrequencyConversion fc = new(_contractFull.FinContract.IdFrequency);
            EMR = fc.CycletoMonthConversion((purchaseRate[0] + cashRate[0] + insurancePaid[0] + _contractFull.CreditCard.CycleFee)) * -1;
            DateTime endFY = startFY.AddYears(1);
            for (int i = 0; i < cf.TotalPayments; i++)
            {
                if (purchaseRate[i] + cashRate[i] <= 0) break;
                // card received payment
                PlanTransaction ratePurchase = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td.AddDays(_contractFull.FinContract.InvoiceNetDays)),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td.AddDays(_contractFull.FinContract.InvoiceNetDays),
                    DebitAccount = _contractFull.FinContract.IdFinAccount,
                    CreditAccount = _contractFull.FinContract.IdPayAccount,
                    TransactionValue = purchaseRate[i],
                    TransactionDetail = detailReceived,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdFinAccount

                };
                _dBContext.PlanTransactions.Add(ratePurchase);
                // card received payment cash
                PlanTransaction rateCash = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td.AddDays(_contractFull.FinContract.InvoiceNetDays)),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td.AddDays(_contractFull.FinContract.InvoiceNetDays),
                    DebitAccount = _contractFull.CreditCard.CashOutAccount,
                    CreditAccount = _contractFull.FinContract.IdPayAccount,
                    TransactionValue = cashRate[i],
                    TransactionDetail = detailReceived,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdFinAccount

                };
                _dBContext.PlanTransactions.Add(rateCash);
                // payee account rate payment
                PlanTransaction ratePay = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td.AddDays(_contractFull.FinContract.InvoiceNetDays)),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td.AddDays(_contractFull.FinContract.InvoiceNetDays),
                    DebitAccount = _contractFull.FinContract.IdFinAccount,
                    CreditAccount = _contractFull.FinContract.IdPayAccount,
                    TransactionValue = purchaseRate[i] + cashRate[i] + insurancePaid[i] + _contractFull.CreditCard.CycleFee,
                    TransactionDetail = detailRate,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdPayAccount

                };
                _dBContext.PlanTransactions.Add(ratePay);
                // interest charge
                PlanTransaction interest = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.CreditCard.InterestAccount,
                    CreditAccount = _contractFull.FinContract.IdFinAccount,
                    TransactionValue = interestPurch[i],
                    TransactionDetail = detailInterest,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdFinAccount
                };
                _dBContext.PlanTransactions.Add(interest);
                PlanTransaction interCO = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.CreditCard.InterestCashOutAcc,
                    CreditAccount = _contractFull.FinContract.IdFinAccount,
                    TransactionValue = interestCash[i],
                    TransactionDetail = detailInterest,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdFinAccount
                };
                _dBContext.PlanTransactions.Add(interCO);
                // card fee charge
                PlanTransaction fee = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                    ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                    TransactionDate = td,
                    DebitAccount = _contractFull.CreditCard.FeeAccount,
                    CreditAccount = _contractFull.FinContract.IdFinAccount,
                    TransactionValue = _contractFull.CreditCard.CycleFee,
                    TransactionDetail = detailFee,
                    ExecutedTransaction = false,
                    IdSource = _contractFull.FinContract.IdFinAccount
                };
                _dBContext.PlanTransactions.Add(fee);
                // card insurance
                if(hasInsurance)
                {
                    PlanTransaction insurance = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(td),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = td,
                        DebitAccount = _contractFull.CreditCard.InsuranceAccount,
                        CreditAccount = _contractFull.FinContract.IdFinAccount,
                        TransactionValue = insurancePaid[i],
                        TransactionDetail = detailRate,
                        ExecutedTransaction = false,
                        IdSource = _contractFull.FinContract.IdFinAccount
                    };
                    _dBContext.PlanTransactions.Add(insurance);
                }
                td = cda.IncrementCycleDate(td);
                if (td > endFY) //next plan transactions will be in the new FY
                {
                    //Add ending FY closure transaction
                    PlanTransaction closeFY = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(endFY.AddDays(-1)),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = endFY.AddDays(-1),
                        DebitAccount = _contractFull.FinContract.IdFinAccount,
                        CreditAccount = userAccount,
                        TransactionValue = balance[i],
                        TransactionDetail = detailClose,
                        ExecutedTransaction = false,
                        IdSource = userAccount
                    };
                    _dBContext.PlanTransactions.Add(closeFY);
                    //Add starting FY opening transaction
                    PlanTransaction openFY = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(endFY),
                        ContractIdentifier = _contractFull.FinContract.IdFinAccount,
                        TransactionDate = endFY,
                        DebitAccount = userAccount,
                        CreditAccount = _contractFull.FinContract.IdFinAccount,
                        TransactionValue = balance[i],
                        TransactionDetail = detailOpen,
                        ExecutedTransaction = false,
                        IdSource = userAccount
                    };
                    _dBContext.PlanTransactions.Add(openFY);
                    //increment FY
                    startFY = endFY;
                    endFY = startFY.AddYears(1);
                }
            }
            return;
        }


    }
}
