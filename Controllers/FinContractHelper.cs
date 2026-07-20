using FinanceCalculator;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using MyFinances.ViewModels;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace MyFinances.Controllers
{
    public class FinContractHelper(AppDBContext dBContext, IConfiguration configuration)
    {
        private readonly AppDBContext _dbContext = dBContext;
        private readonly IConfiguration _configuration = configuration;
        public async Task<ContractFull> GetContractOnID(Guid idc)
        {
            ContractFull contractFull = new();
            if(Guid.TryParse(idc.ToString(), out Guid gc))
            {
                bool[] partsFound = [false, false];
                var fincontract = await _dbContext.FinContracts.FirstOrDefaultAsync(m => m.IdFinContract == idc);
                if (fincontract != null)
                {
                    partsFound[0] = true;
                    contractFull.FinContract = fincontract;
                    switch (fincontract.ContractType)
                    {
                        case 1:
                            var sc_1 = await _dbContext.Deposits.FirstOrDefaultAsync(m => m.IdDeposit == idc);
                            if (sc_1 != null)
                            {
                                partsFound[1] = true;
                                contractFull.Deposit = sc_1;
                            }
                            break;
                        case 2:
                            var sc_2 = await _dbContext.Loans.FirstOrDefaultAsync(m => m.IdLoan == idc);
                            if (sc_2 != null)
                            {
                                partsFound[1] = true;
                                contractFull.Loan = sc_2;
                            }
                            break;
                        case 3:
                            var sc_3 = await _dbContext.CreditCards.FirstOrDefaultAsync(m => m.IdCreditCard == idc);
                            if (sc_3 != null)
                            {
                                partsFound[1] = true;
                                contractFull.CreditCard = sc_3;
                            }
                            break;
                        case 4:
                            var sc_4 = await _dbContext.Employments.FirstOrDefaultAsync(m => m.IdEmployment == idc);
                            if (sc_4 != null)
                            {
                                partsFound[1] = true;
                                contractFull.Employment = sc_4;
                            }
                            break;
                        case 5:
                            var sc_5 = await _dbContext.Incomes.FirstOrDefaultAsync(m => m.IdIncome == idc);
                            if (sc_5 != null)
                            {
                                partsFound[1] = true;
                                contractFull.Income = sc_5;
                            }
                            break;
                        case 6 or 7:
                            var sc_6 = await _dbContext.Bills.FirstOrDefaultAsync(m => m.IdBill == idc);
                            if (sc_6 != null)
                            {
                                partsFound[1] = true;
                                contractFull.Bill = sc_6;
                            }
                            break;
                        case 21: //investment fund
                            var sc_21 = Tracking ? await _dbContext.InvestFunds.FirstOrDefaultAsync(m => m.IdInvestFund == idc) : await _dbContext.InvestFunds.AsNoTracking().FirstOrDefaultAsync(m => m.IdInvestFund == idc);
                            if (sc_21 != null)
                            {
                                partsFound[1] = true;
                                contractFull.InvestFund = sc_21;
                            }
                            break;
                        case 22: //taxation contract
                            break;
                        default:
                            break;
                    }
                }
                if (!(partsFound[0] && partsFound[1])) { contractFull = default!; }
            }
            return contractFull;
        }

        public async Task<decimal> GetContractRealBalance(int idAccount, DateTime referenceDate, int cardCashAccount = 0)
        {
            if(idAccount < 1000000) { return 0; }
            if(referenceDate > DateTime.Now) { referenceDate = DateTime.Now; }
            int accountBalance = cardCashAccount > 0 ? cardCashAccount : idAccount;
            decimal debit = await _dbContext.FinTransactions.Where(p => p.DebitAccount == accountBalance && p.TransactionDate < referenceDate && p.IdSource == idAccount).SumAsync(s => s.TransactionValue);
            decimal credit = await _dbContext.FinTransactions.Where(p => p.CreditAccount == accountBalance && p.TransactionDate < referenceDate && p.IdSource == idAccount).SumAsync(s => s.TransactionValue);
            return debit - credit;
        }

        public async Task<decimal> GetContractPlannedBalance(int idAccount, DateTime referenceDate, int cardCashAccount = 0)
        {
            if (idAccount < 1000000) { return 0; }
            int accountBalance = cardCashAccount > 0 ? cardCashAccount : idAccount;
            decimal debit = await _dbContext.PlanTransactions.Where(p => p.DebitAccount == accountBalance && p.TransactionDate < referenceDate && p.IdSource == idAccount).SumAsync(s => s.TransactionValue);
            decimal credit = await _dbContext.PlanTransactions.Where(p => p.CreditAccount == accountBalance && p.TransactionDate < referenceDate && p.IdSource == idAccount).SumAsync(s => s.TransactionValue);
            return debit - credit;
        }

        public async Task<bool> GetContractExecutionState(Guid idContract)
        {
            var cfa = await _dbContext.FinContracts.Where(p => p.IdFinContract == idContract).Select(s=>s.IdFinAccount).FirstOrDefaultAsync();
            if (cfa == 0) { return false; }
            var tet = await _dbContext.FinTransactions.Where(p=>p.ContractIdentifier == cfa).FirstOrDefaultAsync();
            return tet != null;
        }

        public async Task<bool> GetContractExecutionState(int idAccount)
        {
            var tet = await _dbContext.FinTransactions.Where(p => p.ContractIdentifier == idAccount).FirstOrDefaultAsync();
            return tet != null;
        }

        public async Task<DateTime> GetDateLastTransac(bool hasRecordedTransac, int contractIdentifier, DateTime refDate)
        {
            List<DateTime> tdatesList;
            if (hasRecordedTransac)
            {
                tdatesList = await _dbContext.FinTransactions.Where(p => p.ContractIdentifier == contractIdentifier && p.TransactionDate < refDate).Select(s => s.TransactionDate).OrderBy(o => o).ToListAsync();
            }
            else
            {
                tdatesList = await _dbContext.PlanTransactions.Where(p => p.ContractIdentifier == contractIdentifier && p.TransactionDate < refDate).Select(s => s.TransactionDate).OrderBy(o => o).ToListAsync();
            }
            if (tdatesList.Count > 0)
            {
                return tdatesList.Max();
            }
            else
            {
                return refDate;
            }
        }

        public async Task<List<int>> GetContractAccounts(Guid idContract)
        {
            List<int> result = [];
            var idfa = await _dbContext.FinContracts.Where(p=>p.IdFinContract == idContract).Select(s=>s.IdFinAccount).FirstOrDefaultAsync();
            result.Add(idfa);
            var ctype = await _dbContext.FinContracts.Where(p => p.IdFinContract == idContract).Select(s=>s.ContractType).FirstOrDefaultAsync();
            switch(ctype)
            {
                case 1:
                    var depInterAcc = await _dbContext.Deposits.Where(p => p.IdDeposit == idContract).Select(s => s.InterestAccount).FirstOrDefaultAsync();
                    result.Add(depInterAcc);
                    break;
                case 2:
                    var loanInterAcc = await _dbContext.Loans.Where(p=>p.IdLoan == idContract).Select(s=> s.IdAccountInterest).FirstOrDefaultAsync();
                    result.Add(loanInterAcc);
                    break;
                case 3:
                    var cardAcc = await _dbContext.CreditCards.Where(p => p.IdCreditCard == idContract).Select(s => new { s.InterestAccount, s.CashOutAccount, s.FeeAccount, s.InsuranceAccount}).FirstOrDefaultAsync();
                    if(cardAcc != null)
                    {
                        result.Add(cardAcc.InterestAccount);
                        result.Add(cardAcc.CashOutAccount);
                        result.Add(cardAcc.FeeAccount);
                        result.Add(cardAcc.InsuranceAccount);
                    }
                    break;
            }
            return result;
        }

        public async Task<List<int>> GetNoRecTransacMonths(Guid idContract, DateTime refDate)
        {
            List<int> result = [];
            GeneralHelper gh = new(_dbContext, _configuration);
            DateTime startFY = gh.StartFinancialYear(refDate);
            
            //get contract
            var cont = await _dbContext.FinContracts.FindAsync(idContract);
            if (cont != null)
            {
                List<int> monthsRecorded = [];
                // get months list where the transactions are recorded
                var mtrans = await _dbContext.FinTransactions.Where(p=>p.IdSource == cont.IdFinAccount && p.TransactionDate <= refDate).Select(p => p.TransactionDate.Year * 100 + p.TransactionDate.Month).Distinct().ToListAsync();
                if (mtrans.Count > 0)
                {
                    var x = mtrans.Order();
                    monthsRecorded.AddRange(x);
                }
                // get first invoice of the year
                CalculatorDates calcDate = new(cont.IdFrequency);
                DateTime firstInvoice = calcDate.GetFirstCycleDate(cont.InvoiceDate, startFY);
                DateTime iDate = firstInvoice;
                // build list of months required in the current FY to refDate 
                List<int> monthsRequired = [];
                while (iDate < refDate)
                {
                    monthsRequired.Add(iDate.Year * 100 + iDate.Month);
                    iDate = calcDate.IncrementCycleDate(iDate);
                }
                // add to results monthRequired not found in the monthsRecorded
                foreach (int k in monthsRequired)
                {
                    if(!monthsRecorded.Contains(k))
                    {
                        result.Add(k);
                    }
                }
            }
            return result;
        }

        public async Task<List<int>> GetNoRecTransacMonths(FinContract refContract, DateTime refDate)
        {
            List<int> result = [];
            GeneralHelper gh = new(_dbContext, _configuration);
            DateTime startFY = gh.StartFinancialYear(refDate);

            //get contract
            if (refContract != null)
            {
                List<int> monthsRecorded = [];
                // get months list where the transactions are recorded
                var mtrans = await _dbContext.FinTransactions.Where(p => p.ContractIdentifier == refContract.IdFinAccount && p.TransactionDate <= refDate).Select(p => p.TransactionDate.Year * 100 + p.TransactionDate.Month).Distinct().ToListAsync();
                if (mtrans.Count > 0)
                {
                    var x = mtrans.Order();
                    monthsRecorded.AddRange(x);
                }
                // get first invoice of the year
                CalculatorDates calcDate = new(refContract.IdFrequency);
                DateTime firstInvoice = calcDate.GetFirstCycleDate(refContract.InvoiceDate, startFY);
                DateTime iDate = firstInvoice;
                // build list of months required in the current FY to refDate 
                List<int> monthsRequired = [];
                while (iDate < refDate)
                {
                    monthsRequired.Add(iDate.Year * 100 + iDate.Month);
                    iDate = calcDate.IncrementCycleDate(iDate);
                }
                // add to results monthRequired not found in the monthsRecorded
                foreach (int k in monthsRequired)
                {
                    if (!monthsRecorded.Contains(k))
                    {
                        result.Add(k);
                    }
                }
            }
            return result;
        }

        public static string GetDisplayName<T>(string propertyName)
        {
            var prop = typeof(T).GetProperty(propertyName);

            if (prop == null)
                return propertyName; // fallback

            var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();

            return displayAttr?.Name ?? propertyName;
        }

        public async Task<List<ItemDetail>> GetContractDetailsList(Guid idcont, IMyDetailsService detailsService)
        {
            List<ItemDetail> result = [];
            ContractFull cf = await GetContractOnID(idcont);
            if(cf is null) { return result; }
            // ************************************
            // Fill out the main contract details *
            // ************************************
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.ContractType)), ItemValue = await detailsService.GetContractTypeByID(cf.FinContract.ContractType) });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.ContractName)), ItemValue = cf.FinContract.ContractName });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.ContractDetails)), ItemValue = cf.FinContract.ContractDetails });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdOrganisation)), ItemValue = await detailsService.GetOrganisationNameByID(cf.FinContract.IdOrganisation) });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdContractor)), ItemValue = await detailsService.GetUserNameByID(cf.FinContract.IdContractor) });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.StartDate)), ItemValue = cf.FinContract.StartDate.ToString("d") });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.EndDate)), ItemValue = cf.FinContract.EndDate.ToShortDateString() });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.InvoiceDate)), ItemValue = cf.FinContract.InvoiceDate.ToShortDateString() });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.InvoiceNetDays)), ItemValue = cf.FinContract.InvoiceNetDays.ToString() });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdFrequency)), ItemValue = await detailsService.GetFrequencyNameByID(cf.FinContract.IdFrequency) });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdFinAccount)), ItemValue = cf.FinContract.IdFinAccount.ToString() + " : " + await detailsService.GetAccountNameByID(cf.FinContract.IdFinAccount) });
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdPayAccount)), ItemValue = cf.FinContract.IdPayAccount + ":" +  await detailsService.GetAccountNameByID(cf.FinContract.IdPayAccount) }); //Pay Account
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdBankAccount)), ItemValue = cf.FinContract.IdBankAccount + ":" + await detailsService.GetAccountNameByID(cf.FinContract.IdBankAccount) }); //Bank Account
            result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IsActiveContract)), ItemValue = cf.FinContract.IsActiveContract.ToString() });
            if (cf.FinContract.ClosedDate <= DateTime.Today)
            {
                result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.ClosedDate)), ItemValue = cf.FinContract.ClosedDate.ToShortDateString() });
            }
            else
            {
                result.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.ClosedDate)), ItemValue = "Active contract" });
            }
            // ************************************
            // Fill out the supplementary contract details *
            // ************************************
            switch(cf.FinContract.ContractType)
            {
                case 1: //deposit
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Deposit>(nameof(Deposit.DepositRate)), ItemValue = cf.Deposit.DepositRate.ToString("c", default) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Deposit>(nameof(Deposit.InterestRatePerYear)), ItemValue = cf.Deposit.InterestRatePerYear.ToString() + "%" });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Deposit>(nameof(Deposit.DepositInitialBalance)), ItemValue = cf.Deposit.DepositInitialBalance.ToString("c", default) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Deposit>(nameof(Deposit.DepositRate)), ItemValue = cf.Deposit.InterestAccount.ToString() + ": " + await detailsService.GetAccountNameByID(cf.Deposit.InterestAccount) });
                    break;
                case 2: //loan
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Loan>(nameof(Loan.IdLoanType)), ItemValue = cf.Loan.IdLoanType ? "FIX" : "VARIABLE" });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Loan>(nameof(Loan.LoanAmount)), ItemValue = cf.Loan.LoanAmount.ToString("c", default) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Loan>(nameof(Loan.LoanInterest)), ItemValue = cf.Loan.LoanInterest.ToString() + "%" });
                    if(cf.Loan.LoanSecurity == Guid.Empty || cf.Loan.LoanSecurity is null)
                    {
                        result.Add(new ItemDetail() { ItemName = GetDisplayName<Loan>(nameof(Loan.LoanSecurity)), ItemValue = "Unsecured" });
                    }
                    else
                    {
                        Guid ls = cf.Loan.LoanSecurity ?? Guid.Empty;
                        result.Add(new ItemDetail() { ItemName = GetDisplayName<Loan>(nameof(Loan.LoanSecurity)), ItemValue = await detailsService.GetLoanSecurityNameById(ls) });                        
                    }
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Loan>(nameof(Loan.IdAccountInterest)), ItemValue = cf.Loan.IdAccountInterest + ":" + await detailsService.GetAccountNameByID(cf.Loan.IdAccountInterest) }); //Interest Account
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Loan>(nameof(Loan.ExtraPay)), ItemValue = cf.Loan.ExtraPay.ToString("c", default) });
                    break;
                case 3: //credit card
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.CreditCardLimit)), ItemValue = cf.CreditCard.CreditCardLimit.ToString("c", default) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.CreditCardInterest)), ItemValue = cf.CreditCard.CreditCardInterest.ToString() + "%" });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.CreditCardPayPercent)), ItemValue = cf.CreditCard.CreditCardPayPercent.ToString() + "%" });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.CycleFee)), ItemValue = cf.CreditCard.CycleFee.ToString("c", default) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.CreditCardInsuranceRate)), ItemValue = cf.CreditCard.CreditCardInsuranceRate.ToString() + "%" });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.CashRate)), ItemValue = cf.CreditCard.CashRate.ToString() + "%" });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.ExtraPay)), ItemValue = cf.CreditCard.ExtraPay.ToString("c", default) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.FeeAccount)), ItemValue = cf.CreditCard.FeeAccount + ":" + await detailsService.GetAccountNameByID(cf.CreditCard.FeeAccount) }); //Fee Account
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.InterestAccount)), ItemValue = cf.CreditCard.InterestAccount + ":" + await detailsService.GetAccountNameByID(cf.CreditCard.InterestAccount) }); //Interest Account
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.InterestCashOutAcc)), ItemValue = cf.CreditCard.InterestCashOutAcc + ":" + await detailsService.GetAccountNameByID(cf.CreditCard.InterestCashOutAcc) }); //Cash out Account
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<CreditCard>(nameof(CreditCard.InsuranceAccount)), ItemValue = cf.CreditCard.InsuranceAccount + ":" + await detailsService.GetAccountNameByID(cf.CreditCard.InsuranceAccount) }); //Insurance Account
                    break;
                case 4: //employment
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Employment>(nameof(Employment.GrossPaidCycle)), ItemValue = cf.Employment.GrossPaidCycle.ToString("c", default) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Employment>(nameof(Employment.TaxPaidCycle)), ItemValue = cf.Employment.TaxPaidCycle.ToString("c", default) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Employment>(nameof(Employment.SuperPaidCycle)), ItemValue = cf.Employment.SuperPaidCycle.ToString("c", default) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Employment>(nameof(Employment.IdSuperAccount)), ItemValue = cf.Employment.IdSuperAccount + ":" + await detailsService.GetAccountNameByID(cf.Employment.IdSuperAccount) }); //Super Account
                    break;
                case 5: //income
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Income>(nameof(Income.GrossPerCycle)), ItemValue = cf.Income.GrossPerCycle.ToString("c", default) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Income>(nameof(Income.TaxPaidCycle)), ItemValue = cf.Income.TaxPaidCycle.ToString("c", default) });
                    break;
                case 6 or 7: //bill
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<Bill>(nameof(Bill.BillCycleRate)), ItemValue = cf.Bill.BillCycleRate.ToString("c", default) });
                    break;
                case 21: //investment fund
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.AdviserFee)), ItemValue = cf.InvestFund.AdviserFee.ToString() });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.ManagementFee)), ItemValue = cf.InvestFund.ManagementFee.ToString() });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.AdminFee)), ItemValue = cf.InvestFund.AdminFee.ToString() });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.AccountingFee)), ItemValue = cf.InvestFund.AccountingFee.ToString() });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdCapitalGainAcc)), ItemValue = cf.InvestFund.IdCapitalGainAcc.ToString() + " : " + await detailsService.GetAccountNameByID(cf.InvestFund.IdCapitalGainAcc) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdAdviseAcc)), ItemValue = cf.InvestFund.IdAdviseAcc.ToString() + " : " + await detailsService.GetAccountNameByID(cf.InvestFund.IdAdviseAcc) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdManageAcc)), ItemValue = cf.InvestFund.IdManageAcc.ToString() + " : " + await detailsService.GetAccountNameByID(cf.InvestFund.IdManageAcc) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdAdminAcc)), ItemValue = cf.InvestFund.IdAdminAcc.ToString() + " : " + await detailsService.GetAccountNameByID(cf.InvestFund.IdAdminAcc) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdAccountingAcc)), ItemValue = cf.InvestFund.IdAccountingAcc.ToString() + " : " + await detailsService.GetAccountNameByID(cf.InvestFund.IdAccountingAcc) });
                    result.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdTaxAccount)), ItemValue = cf.InvestFund.IdTaxAccount.ToString() + " : " + await detailsService.GetAccountNameByID(cf.InvestFund.IdTaxAccount) });
                    break;
            }
            return result;
        }


    }
}
