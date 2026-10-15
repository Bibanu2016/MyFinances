using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;

namespace MyFinances.Pages.FinTransactions
{
    public class ExecuteDueTransactionModel(AppDBContext dBContext, IComboFillService fillService) : PageModel
    {
        private readonly AppDBContext _dbContext = dBContext;
        private readonly IComboFillService _fillService = fillService;

        [BindProperty]
        public PlanTransaction DueTransaction { get; set; } = default!;

        public int TotalExecTransactions { get; set; } = 0;
        public int TotalPlanTransactions { get; set; } = 0;
        public SelectList AccList { get; set; } = default!;
        public async Task<IActionResult> OnGet(long id)
        {
            var opt = await _dbContext.PlanTransactions.FindAsync(id);
            if (opt is null) { return NotFound(); }
            DueTransaction = opt;
            AccList = await _fillService.GetFinancialAccountsList();
            TotalExecTransactions = await CountTotalExecTransac(DueTransaction);
            TotalPlanTransactions = await CountTotalPlanTransac(DueTransaction);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _dbContext.PlanTransactions.Update(DueTransaction);
            int r = await _dbContext.SaveChangesAsync();
            if (r == 1) { TempData["SuccessMessage"] = $"Planned transaction with ID {DueTransaction.IdTransaction} has been executed"; }
            return RedirectToPage("OverduePlanTransactions");
        }

        private async Task<int> CountTotalExecTransac(PlanTransaction refTra)
        {
            if (refTra == null) { return 0; }
            return await _dbContext.FinTransactions.Where(p =>
            p.DebitAccount == refTra.DebitAccount &&
            p.CreditAccount == refTra.CreditAccount &&
            p.IdSource == refTra.IdSource &&
            p.ContractIdentifier == refTra.ContractIdentifier &&
            p.TransactionDate <= refTra.TransactionDate)
                .CountAsync();
        }

        private async Task<int> CountTotalPlanTransac(PlanTransaction refTra)
        {
            if (refTra == null) { return 0; }
            return await _dbContext.PlanTransactions.Where(p =>
            p.DebitAccount == refTra.DebitAccount &&
            p.CreditAccount == refTra.CreditAccount &&
            p.IdSource == refTra.IdSource &&
            p.ContractIdentifier == refTra.ContractIdentifier &&
            p.TransactionDate <= refTra.TransactionDate)
                .CountAsync();
        }
    }
}
