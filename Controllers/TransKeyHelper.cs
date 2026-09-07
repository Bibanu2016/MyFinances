using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Controllers
{
    public class TransKeyHelper(AppDBContext dbContext)
    {
        private readonly AppDBContext _dbContext = dbContext;

        public async Task UpdateTransactionPairAcc(int accID, bool isDebit, string keyDetail, int fillAcc)
        {
            List<FinTransaction> TList = [];
            if(isDebit)
            {
                var db = await _dbContext.FinTransactions
                    .Where(t => t.DebitAccount == accID && t.CreditAccount == 0 && t.TransactionDetail.Contains(keyDetail))
                    .ToListAsync();
                foreach (var t in db)
                {
                    t.CreditAccount = fillAcc;
                }
                TList.AddRange(db);
            }
            else
            {
                var cl = await _dbContext.FinTransactions
                    .Where(t => t.CreditAccount == accID && t.DebitAccount == 0 && t.TransactionDetail.Contains(keyDetail))
                    .ToListAsync();
                foreach (var t in cl)
                {
                    t.DebitAccount = fillAcc;
                }
                TList.AddRange(cl);
            }
            foreach(FinTransaction fint in TList)
            {
                _dbContext.FinTransactions.Update(fint);
            }
            return;
        }
    }
}
