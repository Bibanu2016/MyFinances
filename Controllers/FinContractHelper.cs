using FinanceCalculator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.ViewModels;
using System.ComponentModel;
using System.Diagnostics.Contracts;

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

    }
}
