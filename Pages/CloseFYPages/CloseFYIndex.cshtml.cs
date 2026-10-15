using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;

namespace MyFinances.Pages.CloseFYPages
{
    public class CloseFYIndexModel(AppDBContext dBContext, IConfiguration myconfig) : PageModel
    {
        private readonly AppDBContext _dbContext = dBContext;
        private readonly IConfiguration _config = myconfig;

        [BindProperty]
        public DateTime EndOfFY { get; set; } = DateTime.Now;

        [BindProperty]
        public bool LastFYClosed { get; set; } = false;

        [BindProperty]
        public int OverdueTransactionsCount { get; set; } = 0;

        public async Task OnGet()
        {
            GeneralHelper gh = new(_config);
            DateTime startOfFY = gh.StartFinancialYear(DateTime.Now);
            EndOfFY = startOfFY.AddDays(-1); // Set EndOfFY to the last day of the previous financial year
            OverdueTransactionsCount = await _dbContext.PlanTransactions.AsQueryable().Where(m => m.TransactionDate <= EndOfFY && !m.ExecutedTransaction).AsNoTracking().CountAsync();
            int lastFY = _config.GetSection("UserSettings").GetValue<int>("LastFY");
            LastFYClosed = lastFY == EndOfFY.Year;
            ViewData["Title"] = $"Close Financial Year {EndOfFY.Year}";
            return;
        }
    }
}
