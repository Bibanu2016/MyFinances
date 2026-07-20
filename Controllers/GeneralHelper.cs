using FinanceCalculator;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Controllers
{
    public class GeneralHelper(AppDBContext context, IConfiguration configuration)
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;

        public DateTime StartFinancialYear(DateTime referenceDate)
        {
            //by default set the date as the last day of the current year
            string x;
            DateTime result = DateTime.Parse(referenceDate.Year.ToString() + "-01-01");
            var m = _configuration.GetSection("MySettings").GetValue<int>("MonthStartFY");
            if(m > 0 && m < 13)
            {
                if(referenceDate.Month < m)
                {
                    x = (referenceDate.Year - 1).ToString();
                }
                else
                {
                    x= referenceDate.Year.ToString();
                }
                string ms = m < 10?("0" + m.ToString()):m.ToString();
                result = DateTime.Parse(x + "-" + ms + "-01");
            }
            return result;
        }

        public DateTime EndPeriodPlanTransaction(DateTime referenceDate, DateTime endDate, short idEnd)
        {
            DateTime startFY = StartFinancialYear(referenceDate);
            int x = _configuration.GetSection("MySettings").GetValue<int>("YearsPlanning");
            int y = _configuration.GetSection("MySettings").GetValue<int>("MaxYearPlanning");
            if(y == 0) { y = 5; } // guard against a zero value for maximum planning
            if(x > y) { x = y; } // guard for errors where the normal plan term is set greater than maximum
            DateTime temp = startFY.AddYears(x + 1);
            if (idEnd == 0) 
            {
                //contract terminates on the End date
                return temp >= endDate ? endDate : temp;
            }
            else
            {
                //contract renews or is prpetual - terminate on the set YearsPlanning
                return temp;
            }
        }

        public async Task<decimal> GetContractLastBalance(DateTime referenceDate, int accountID)
        {
            decimal debitBalance = await _context.FinTransactions.Where(p => p.DebitAccount == accountID && p.TransactionDate <= referenceDate).SumAsync(a => a.TransactionValue);
            decimal creditBalance = await _context.FinTransactions.Where(p => p.CreditAccount == accountID && p.TransactionDate <= referenceDate).SumAsync(a => a.TransactionValue);
            return debitBalance - creditBalance;
        }

        public async Task<decimal[]> GetCreditCardLastBalance(DateTime referenceDate, int accountID, int cashoutAccId)
        {
            decimal debitBalance = await _context.FinTransactions.Where(p => p.DebitAccount == accountID && p.TransactionDate <= referenceDate).SumAsync(a => a.TransactionValue);
            decimal creditBalance = await _context.FinTransactions.Where(p => p.CreditAccount == accountID && p.TransactionDate <= referenceDate).SumAsync(a => a.TransactionValue);
            decimal dcoBalance = await _context.FinTransactions.Where(p => p.DebitAccount == cashoutAccId && p.TransactionDate <= referenceDate).SumAsync(a => a.TransactionValue);
            decimal ccoBalance = await _context.FinTransactions.Where(p => p.CreditAccount == cashoutAccId && p.TransactionDate <= referenceDate).SumAsync(a => a.TransactionValue);
            return
            [
                debitBalance - creditBalance, dcoBalance - ccoBalance
            ];
        }

        public async Task<RealBalance> GetContractLastPlanBalance(DateTime refDate, int accountID, int idFreq, DateTime invoiceDate, bool isCreditCard = false)
        {
            RealBalance result = new();
            CalculatorDates cd = new(idFreq);
            DateTime lastBalanceDate = cd.LastCycleDate(invoiceDate, refDate);
            if(isCreditCard)
            {
                var lbc = await _context.BalanceCards.AsQueryable().Where(x => x.IdAccount == accountID && x.DateBalance >= lastBalanceDate && x.DateBalance <= refDate).OrderBy(y=>y.DateBalance).ToListAsync();
                if(lbc.Count > 0)
                {
                    result.BalanceValue = lbc[^1].TotalBalance;
                    result.CashOutValue = lbc[^1].CashlBalance;
                    result.BalanceDate = lbc[^1].DateBalance;
                }
            }
            else
            {
                var lbc = await _context.Balances.AsQueryable().Where(x => x.IdFinAccount == accountID && x.BalanceDate >= lastBalanceDate && x.BalanceDate <= refDate).OrderBy(y => y.BalanceDate).ToListAsync();
                if (lbc.Count > 0)
                {
                    result.BalanceValue = lbc[^1].BalanceValue;
                    result.BalanceDate = lbc[^1].BalanceDate;
                }
            }
            return result;
        }

        public async Task<int> GetOrganisationId(short categoryId)
        {
            int result = 0;
            var losc = await _context.Organisations.Where(x =>x.IdCateg == categoryId).OrderBy(y =>y.IdOrg).AsNoTracking().ToListAsync();
            if (losc.Count > 0)
            {
                int idx = categoryId * 1000;
                for (int i = 0; i < losc.Count; i++)
                {
                    if (losc[i].IdOrg > (idx + 1))
                    {
                        result = idx + 1; break;
                    }
                    idx++;
                }
                if (result == 0) { result = categoryId * 1000 + idx; }
            }
            else
            {
                result = categoryId * 1000 + 1;
            }
            return result ;
        }
    }
}
