using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using MyFinances.Data;
using MyFinances.Pages.Contracts;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace MyFinances.Controllers
{
    public class FinAccountHelper(AppDBContext context, IConfiguration configuration)
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;

        public async Task<int[]> NewIdAccountByCateg(int categId, bool two = false)
        {
            int[] result = [0,0];
            var cFound = await _context.AccountCategories.FindAsync(categId);
            if(cFound is null) { return result; }
            var accList = await _context.FinAccounts.AsQueryable().Where(x => x.AccountCategory == categId).OrderBy(y => y.IdFinAccount).AsNoTracking().ToListAsync();
            if(accList.Count >= 999) { return result; } // guard against overflow
            if (accList.Count > 0)
            {
                // search for any gaps in the sequence - if found allocate the number
                int cx = categId * 1000;
                bool found = false;
                if (two)
                {
                    // allocated the last number
                    result[0] = accList[^1].IdFinAccount + 1;
                    result[1] = accList[^1].IdFinAccount + 2;
                }
                else
                {
                    for (int i = 0; i < accList.Count; i++)
                    {
                        if (accList[i].IdFinAccount != cx + i + 1)
                        {
                            found = true;
                            result[0] = cx + i + 1;
                            break;
                        }
                    }
                    if (!found)
                    {
                        result[0] = accList[^1].IdFinAccount + 1;
                    }
                }
            }
            else
            {
                result[0] = categId * 1000 + 1;
                result[1] = categId * 1000 + 2;
            }
            return result;
        }

        public async Task<bool> AccountIsUsedByTransactions(int accountId)
        {
            var aFound = await _context.FinAccounts.FindAsync(accountId);
            if (aFound != null)
            {
                var y = await _context.FinTransactions.Where(x=>x.DebitAccount == accountId || x.CreditAccount == accountId).AsNoTracking().FirstOrDefaultAsync();
                var x = await _context.PlanTransactions.Where(x => (x.DebitAccount == accountId || x.CreditAccount == accountId) && x.ContractIdentifier == 0).AsNoTracking().FirstOrDefaultAsync();
                if (y != null || x != null)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> FinAccountUsedByUSer(int accountId)
        {
            bool result = false;
            result = await _context.UserContracts.Where(p => p.UserAccountID == accountId || p.UserTaxAccount == accountId).AnyAsync();
            if(!result)
            {
                result = await _context.UserContracts.Where(p => p.UserPayable == accountId || p.UserReceivable == accountId).AnyAsync();
            }
            if(!result)
            {
                result = await _context.UserContracts.Where(p => p.UserCash == accountId).AnyAsync();
            }
            return result;
        }

        public async Task<bool> FinAccountUsedByTAsset(int accountId)
        {
            if(accountId < 2000000)
            {
                return await _context.TangibleAssets.Where(p=>p.IdFinAccount == accountId).AnyAsync();
            }
            return false;
        }

        public async Task<bool> FinAccountUsedByShares(int accountId)
        {
            if (accountId < 2000000)
            {
                return await _context.SharePortfolios.Where(p=>p.IdFinAccount == accountId).AnyAsync();
            }
            return false;
        }

        public async Task<bool> FinAccountUsedByTransacKeys(int accountId)
        {
            var aFound = await _context.FinAccounts.FindAsync(accountId);
            return await _context.TransacDictionary.Where(p => p.IdSource == accountId || p.IdTarget == accountId).AnyAsync();
        }

        /// <summary>
        /// Checks if a financial account is used in any of the recorded contracts
        /// </summary>
        /// <param name="accID">account identifier as integer between 1000000 and 7999999</param>
        /// <returns>True if the accounts is recorded in any of the contracts</returns>
        public async Task<bool> AccountUsedInContract(int accID)
        {
            bool result = false;
            if (accID < 1000000 || accID >= 8000000) { return result; } // guard against input errors
            var auc = await _context.FinContracts.Where(p => p.IdFinAccount == accID || p.IdPayAccount == accID || p.IdBankAccount == accID).AsNoTracking().FirstOrDefaultAsync();
            result = auc != null;
            if(!result)
            {
                // Deposit - Interest received
                var dep = await _context.Deposits.AsNoTracking().FirstOrDefaultAsync(p => p.InterestAccount == accID);
                result = dep != null;
            }
            if(!result)
            {
                //Loan - Interest paid
                var loan = await _context.Loans.AsNoTracking().FirstOrDefaultAsync(p => p.IdAccountInterest == accID);
                result = loan != null;
            }
            if (!result)
            {
                // Credit card - Interest paid, Interest paid (CO), Insurance, Cash, Fee
                // Credit card - Interest paid, Interest paid (CO), Insurance, Cash, Fee
                var card_1 = await _context.CreditCards.AsNoTracking().Where(p => p.InterestAccount == accID || p.InterestCashOutAcc == accID).FirstOrDefaultAsync();
                var card_2 = await _context.CreditCards.Where(p => p.InsuranceAccount == accID || p.CashOutAccount == accID || p.FeeAccount == accID).AsNoTracking().FirstOrDefaultAsync();
                result = card_1 != null && card_2 != null;
            }
            if (!result)
            {
                // Employment - Superannuation
                var employ = await _context.Employments.FirstOrDefaultAsync(p => p.IdSuperAccount == accID);
                result= employ != null;
            }
            return result;
        }

        public async Task<bool> AccountIsContract(int accID)
        {
            return await _context.FinContracts.Where(p => p.IdFinAccount == accID).AsNoTracking().FirstOrDefaultAsync() != null;
        }

        public async Task<bool> AccountIsSource(int accID)
        {
            bool result = false;
            // get account category
            var categFA = await _context.FinAccounts.Where(p=>p.IdFinAccount == accID).Select(s=>s.AccountCategory).FirstOrDefaultAsync();
            if(categFA >= 1000)
            {
                var acr = await _context.AccountCategories.Where(p=>p.IdAccountCategory == categFA).Select(s=>s.CategoryRoles).FirstOrDefaultAsync();
                if(acr != null)
                {
                    result = MyBitOperations.GetBit(acr[0], 2);
                }
            }
            return result;
        }
    }
}
