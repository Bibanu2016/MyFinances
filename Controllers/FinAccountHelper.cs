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
            var maxAcc = await _context.FinAccounts.AsQueryable().Where(x => x.AccountCategory == categId).CountAsync();
            if (maxAcc > 0) 
            {
                // guard against overflow
                if (maxAcc >= 999)
                {
                    // value reached maximum for the category - check if there are any gaps in the sequence
                    int temp = categId * 1000;
                    var anl = await _context.FinAccounts.Select(s=>s.IdFinAccount).OrderBy(o => o).ToListAsync();
                    int maxcount = 999 > anl.Count ? anl.Count : 999;
                    int startiter = 1;
                    for (int i = startiter; i <= maxcount; i++) // value asigned as the maximum for the category is 999
                    {
                        temp += i;
                        if (anl[i-1] != temp) { result[0] = temp; startiter = i + 1; break; }
                    }
                    if(two) // start looking for the second gap after the first one was found
                    {
                        temp += 1;
                        for (int i = startiter; i <= maxcount; i++) // value asigned as the maximum for the category is 999
                        {
                            temp += i;
                            if (anl[i - 1] != temp) { result[1] = temp; startiter = i + 1; break; }
                        }
                    }
                }
                else
                {
                    result[0] = categId * 1000 + maxAcc + 1;
                    if (two) { result[1] = categId * 1000 + maxAcc + 2; }
                }
            }
            else
            { 
                result[0] = categId * 1000 + 1; 
                if(two) { result[1] = categId * 1000 + 2; }
            }
            return result;
        }

        public async Task<bool> AccountIsUsedByTransactions(int accountId)
        {
            var y = await _context.FinTransactions.Where(x => x.DebitAccount == accountId || x.CreditAccount == accountId).AnyAsync();
            var x = await _context.PlanTransactions.Where(x => x.DebitAccount == accountId || x.CreditAccount == accountId).AnyAsync();
            return x || y;
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
            result = await _context.FinContracts.Where(p => p.IdFinAccount == accID || p.IdPayAccount == accID || p.IdBankAccount == accID).AnyAsync();
            int accCateg = accID / 1000;
            IConfigurationSection acs = _configuration.GetSection("AccountCategoriesSettings");
            if (!result)
            {
                
                int card = acs.GetValue<int>("CardCategory") * 100 + 1;
                card = card == 0 ? 2101 : card; // guard against missing configuration value
                if (accCateg == card)
                {
                    result = await _context.CreditCards.Where(p => p.CashOutAccount == accID).AnyAsync();
                }
            }
            if(!result)
            {
                int intrec = acs.GetValue<int>("InterestReceived");
                intrec = intrec == 0 ? 4102 : intrec; // guard against missing configuration value
                if(accCateg == intrec)
                {
                    result = await _context.Deposits.Where(p => p.InterestAccount == accID).AnyAsync();
                }
            }
            if(!result)
            {
                int intpay = acs.GetValue<int>("InterestPaid");
                intpay = intpay == 0 ? 5004 : intpay; // guard against missing configuration value
                if (accCateg == intpay)
                {
                    bool x = await _context.Loans.Where(p => p.IdAccountInterest == accID).AnyAsync();
                    bool y = await _context.CreditCards.Where(p => p.InterestAccount == accID).AnyAsync();
                    bool z = await _context.CreditCards.Where(p => p.InterestCashOutAcc == accID).AnyAsync();
                    result = x || y || z;
                }
            }
            if(!result)
            {
                int fees = acs.GetValue<int>("Fees");
                fees = fees == 0 ? 5005 : fees; // guard against missing configuration value
                if (accCateg == fees)
                {
                    result = await _context.CreditCards.Where(p => p.FeeAccount == accID).AnyAsync();
                }
            }
            if(!result)
            {
                int isur = acs.GetValue<int>("InsuranceCredit");
                isur = isur == 0 ? 5006 : isur; // guard against missing configuration value
                if(accCateg == isur)
                {
                    result = await _context.CreditCards.Where(p => p.InsuranceAccount == accID).AnyAsync();
                }
            }
            if (!result)
            {
                int fundcg = acs.GetValue<int>("FundCapitalGain");
                fundcg = fundcg == 0 ? 4108 : fundcg; // guard against missing configuration value
                if(accCateg == fundcg)
                {
                    result = await _context.InvestFunds.Where(p => p.IdCapitalGainAcc == accID).AnyAsync();
                }
            }
            if (!result)
            {
                int fundmf = acs.GetValue<int>("FundManagementFee");
                fundmf = fundmf == 0 ? 5201 : fundmf; // guard against missing configuration value
                if(accCateg == fundmf)
                {
                    result = await _context.InvestFunds.Where(p => p.IdManageAcc == accID).AnyAsync();
                }
            }
            if (!result)
            {
                int fundadmin = acs.GetValue<int>("FundAdminFee");
                fundadmin = fundadmin == 0 ? 5202 : fundadmin; // guard against missing configuration value
                if(accCateg == fundadmin)
                {
                    result = await _context.InvestFunds.Where(p => p.IdAdminAcc == accID).AnyAsync();
                }
            }
            if (!result)
            {
                int fundadviser = acs.GetValue<int>("FundAdviserFee");
                fundadviser = fundadviser == 0 ? 5203 : fundadviser; // guard against missing configuration value
                if(accCateg == fundadviser)
                {
                    result = await _context.InvestFunds.Where(p => p.IdAdviseAcc == accID).AnyAsync();
                }
            }
            if (!result)
            {
                int fundaccount = acs.GetValue<int>("FundAccountingFee");
                fundaccount = fundaccount == 0 ? 5206 : fundaccount; // guard against missing configuration value
                if(accCateg == fundaccount)
                {
                    result = await _context.InvestFunds.Where(p => p.IdAccountingAcc == accID).AnyAsync();
                }
            }
            return result;
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

        public async Task<bool> AccountIsUsed(int accountID)
        {
            bool result = await AccountIsUsedByTransactions(accountID);
            if (!result)
            {
                result = await FinAccountUsedByUSer(accountID);
            }
            if (!result)
            {
                result = await FinAccountUsedByTAsset(accountID);
            }
            if (!result)
            {
                result = await FinAccountUsedByShares(accountID);
            }
            if (!result)
            {
                result = await FinAccountUsedByTransacKeys(accountID);
            }
            if (!result)
            {
                result = await AccountUsedInContract(accountID);
            }
            return result;
        }
    }
}
