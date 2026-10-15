using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;

namespace MyFinances.Pages.FinTransactions
{
    public class AccountTransactionsModel(AppDBContext appDBContext, IComboFillService comboFill) : PageModel
    {
        private readonly AppDBContext _appDbContext = appDBContext;
        private readonly IComboFillService _comboFill = comboFill;

        [BindProperty(SupportsGet = true)]
        public DateTime FilterStartdate { get; set; } = DateTime.Now.AddDays(-30);

        [BindProperty(SupportsGet = true)]
        public DateTime FilterEnddate { get; set; } = DateTime.Now.AddDays(30);

        [BindProperty(SupportsGet = true)]
        public int FilterAccount { get; set; } = 0;

        public decimal[] categorySums = [0, 0, 0];

        public SelectList AccountsList { get; set; } = default!;

        public IList<ViewModels.AccountLedger> APlanned { get; set; } = [];
        public IList<ViewModels.AccountLedger> AOverdue { get; set; } = [];
        public IList<ViewModels.AccountLedger> ARecorded { get; set; } = [];

        public async Task OnGetAsync(string text)
        {
            AccountsList = await _comboFill.GetFinancialAccountsList();
            DateTime fromDate = FilterStartdate <= FilterEnddate ? FilterStartdate : FilterEnddate;
            DateTime toDate = FilterEnddate >= FilterStartdate ? FilterEnddate : FilterStartdate;
            if (FilterAccount > 0)
            {
                //planned transactions
                string accountName = string.Empty;
                string contractName = string.Empty;
                string sourceName = string.Empty;
                var aptl = await _appDbContext.PlanTransactions
                    .Where(p => p.TransactionDate >= DateTime.Today && p.TransactionDate <= toDate && (p.DebitAccount == FilterAccount || p.CreditAccount == FilterAccount))
                    .AsNoTracking().OrderByDescending(o => o.TransactionDate).ToListAsync();
                if (aptl.Count > 0)
                {
                    List<PlanTransaction> onlySource = [.. aptl.Where(p => p.IdSource == FilterAccount || p.IdSource == 0)];
                    foreach (var pt in onlySource)
                    {
                        ViewModels.AccountLedger al = new()
                        {
                            Id = pt.IdTransaction,
                            DateTransaction = DateOnly.FromDateTime(pt.TransactionDate),
                            Details = pt.TransactionDetail,
                        };
                        int signT = pt.DebitAccount == FilterAccount ? 1 : -1;
                        if (signT > 0)
                        {
                            accountName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.CreditAccount.ToString())?.Text ?? string.Empty;
                        }
                        else
                        {
                            accountName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.DebitAccount.ToString())?.Text ?? string.Empty;
                        }
                        contractName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.ContractIdentifier.ToString())?.Text ?? string.Empty;
                        sourceName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.IdSource.ToString())?.Text ?? string.Empty;
                        al.AccountName = accountName;
                        al.ContractName = contractName;
                        al.SourceName = sourceName;
                        al.Amount = signT == 1 ? pt.TransactionValue : signT * pt.TransactionValue;
                        APlanned.Add(al);
                        categorySums[0] += al.Amount;
                    }
                }
                //
                //overdue transactions
                var optl = await _appDbContext.PlanTransactions
                    .Where(p => p.ExecutedTransaction == false && p.TransactionDate < DateTime.Today && (p.DebitAccount == FilterAccount || p.CreditAccount == FilterAccount))
                    .AsNoTracking().OrderByDescending(o => o.TransactionDate).ToListAsync();
                if (optl.Count > 0)
                {
                    List<PlanTransaction> onlySource = [.. optl.Where(p => p.IdSource == FilterAccount || p.IdSource == 0)];
                    foreach (var pt in onlySource)
                    {
                        ViewModels.AccountLedger al = new()
                        {
                            Id = pt.IdTransaction,
                            DateTransaction = DateOnly.FromDateTime(pt.TransactionDate),
                            Details = pt.TransactionDetail,
                        };
                        int signT = pt.DebitAccount == FilterAccount ? 1 : -1;
                        if (signT > 0)
                        {
                            accountName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.CreditAccount.ToString())?.Text ?? string.Empty;
                        }
                        else
                        {
                            accountName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.DebitAccount.ToString())?.Text ?? string.Empty;
                        }
                        contractName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.ContractIdentifier.ToString())?.Text ?? string.Empty;
                        sourceName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.IdSource.ToString())?.Text ?? string.Empty;
                        al.AccountName = accountName;
                        al.ContractName = contractName;
                        al.SourceName = sourceName;
                        al.Amount = signT == 1 ? pt.TransactionValue : signT * pt.TransactionValue;
                        AOverdue.Add(al);
                        categorySums[1] += al.Amount;
                    }
                }
                //
                //recorded transactions
                var reptl = await _appDbContext.FinTransactions
                    .Where(p => p.TransactionDate >= fromDate && p.TransactionDate < DateTime.Today && (p.DebitAccount == FilterAccount || p.CreditAccount == FilterAccount))
                    .AsNoTracking().OrderByDescending(o => o.TransactionDate).ToListAsync();
                if (reptl.Count > 0)
                {
                    List<FinTransaction> onlySource = [.. reptl.Where(p => p.IdSource == FilterAccount || p.IdSource == 0)];
                    foreach (var pt in onlySource)
                    {
                        ViewModels.AccountLedger al = new()
                        {
                            Id = pt.IdTransaction,
                            DateTransaction = DateOnly.FromDateTime(pt.TransactionDate),
                            Details = pt.TransactionDetail,
                        };
                        int signT = pt.DebitAccount == FilterAccount ? 1 : -1;
                        if (signT > 0)
                        {
                            accountName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.CreditAccount.ToString())?.Text ?? string.Empty;
                        }
                        else
                        {
                            accountName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.DebitAccount.ToString())?.Text ?? string.Empty;
                        }
                        contractName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.ContractIdentifier.ToString())?.Text ?? string.Empty;
                        sourceName = AccountsList.FirstOrDefault(predicate => predicate.Value == pt.IdSource.ToString())?.Text ?? string.Empty;
                        al.Amount = signT == 1 ? pt.TransactionValue : signT * pt.TransactionValue;
                        al.AccountName = accountName;
                        al.ContractName = contractName;
                        al.SourceName = sourceName;
                        ARecorded.Add(al);
                        categorySums[2] += al.Amount;
                    }
                }
            }
        }
    }
}
