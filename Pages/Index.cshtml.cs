using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using System.Text;

namespace MyFinances.Pages
{
    public class IndexModel(AppDBContext dBContext, IConfiguration configuration) : PageModel
    {
        private readonly AppDBContext _dbContext = dBContext;
        private readonly IConfiguration _config = configuration;

        public MyHomePage MHPg { set; get; } = default!;
        public List<ViewModels.ItemDetail> LastImpAccList { private set; get; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            MHPg = new MyHomePage();
            MHPg.UserName = Environment.UserName;
            await FillOutData();
            return Page();
        }

        private async Task FillOutData()
        {
            DateTime sfy = new GeneralHelper(_config).StartFinancialYear(DateTime.Today);
            MHPg.FY = sfy.Year.ToString() + "-" + (sfy.Year + 1).ToString();
            double td = (sfy.AddYears(1) - DateTime.Today).TotalDays;
            if (td <= int.MaxValue) { MHPg.DaysLeftFY = (int)td; }
            MHPg.ActiveContracts = (short)(await _dbContext.FinContracts.Where(p => p.IsActiveContract == true).CountAsync());
            MHPg.ClosedContracts = (short)(await _dbContext.FinContracts.Where(p => p.IsActiveContract == false).CountAsync());
            MHPg.EMCshFlow = await _dbContext.FinContracts.Where(p => p.IsActiveContract).SumAsync(s => s.EquivMonthlyRate);
            var list7d = await _dbContext.PlanTransactions.Where(p => p.TransactionDate >= DateTime.Today && p.TransactionDate <= DateTime.Today.AddDays(7)).Select(p => new { p.DebitAccount, p.CreditAccount, p.TransactionValue }).ToListAsync();
            if (list7d.Count > 0)
            {
                foreach (var item in list7d)
                {
                    if (item.DebitAccount.ToString().StartsWith("1002")) { MHPg.F7InCash += (int)item.TransactionValue; }
                    if (item.CreditAccount.ToString().StartsWith("1002")) { MHPg.F7OutCash += (int)item.TransactionValue; }
                }
            }
            MHPg.NoDueTransac = await _dbContext.PlanTransactions.Where(p => p.TransactionDate < DateTime.Today && !p.ExecutedTransaction).CountAsync();
            var dueacc = await _dbContext.PlanTransactions.Where(p => p.TransactionDate < DateTime.Today && !p.ExecutedTransaction).Select(s => s.ContractIdentifier).Distinct().ToListAsync();
            if (dueacc.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (int item in dueacc) { sb.Append(item); sb.Append(", "); }
                MHPg.DueAccounts = sb.ToString();
            }
            MHPg.Exec7days = await _dbContext.PlanTransactions.Where(p => p.TransactionDate < DateTime.Today && p.TransactionDate > DateTime.Today.AddDays(-7) && p.ExecutedTransaction).CountAsync();
            var odfy = await _dbContext.PlanTransactions.Where(p => p.TransactionDate < sfy).CountAsync();
            if (odfy > 0)
            {
                double dffy = (DateTime.Today - sfy).TotalDays;
                if (dffy < int.MaxValue) { MHPg.FYClosure = (int)dffy; }
            }
        }

    }

    public class MyHomePage()
    {
        public string UserName { set; get; } = string.Empty;
        public string FY { set; get; } = "20xx-20yy";
        public int DaysLeftFY { set; get; } = 0;
        public short ActiveContracts { set; get; } = 0;
        public short ClosedContracts { set; get; } = 0;
        public int EMCshFlow { set; get; } = 0;
        public int F7InCash { set; get; } = 0;
        public int F7OutCash { set; get; } = 0;
        public int NoDueTransac { set; get; } = 0;
        public string DueAccounts { set; get; } = string.Empty;
        public int Exec7days { set; get; } = 0;
        public int FYClosure { set; get; } = -1;
        public bool USHealth { set; get; } = false;
    }
}
