using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.Data.Common;

namespace MyFinances.Pages.FinTransactions
{
    public class DeletePlanTransactionModel(AppDBContext dBContext, IComboFillService fillService) : PageModel
    {
        private readonly AppDBContext _appDbContext = dBContext;
        private readonly IComboFillService _fillService = fillService;

        [BindProperty]
        public PlanTransaction MyPlanTransaction { get; set; } = default!;

        public SelectList FAList { get; set; } = new SelectList("0", "0");

        public async Task<IActionResult> OnGet(int id)
        {
            var pt = await _appDbContext.PlanTransactions.FindAsync((long)id);
            if (pt == null) { return NotFound(); }
            MyPlanTransaction = pt;
            FAList = await _fillService.GetFinancialAccountsList();
            ViewData["Title"] = $"Delete plan transaction id: {MyPlanTransaction.IdTransaction}?";
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            _appDbContext.PlanTransactions.Remove(MyPlanTransaction);
            try
            {
                await _appDbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Plan transaction with id: {MyPlanTransaction.IdTransaction} was deleted successfuly";

            }
            catch (DbException dex)
            {
                TempData["ErrorMessage"] = $"Ooops there was an error with processing this transaction. \n {dex.Message}";
            }
            return RedirectToPage("FutureTransactions");
        }
    }
}
