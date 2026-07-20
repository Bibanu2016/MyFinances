using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;

namespace MyFinances.Pages.FinTransactions
{
    public class AddPlanTransactionModel(AppDBContext context, IComboFillService fillService, IMemoryCache cache) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IComboFillService _fillService = fillService;
        private readonly IMemoryCache _cache = cache;
        public async Task<IActionResult> OnGet()
        {
            FinAccList = await _cache.GetOrCreateAsync("FAL", () => GetFAL());
            return Page();
        }

        [BindProperty]
        public PlanTransaction PlanTransaction { get; set; } = default!;

        public SelectList FinAccList { get; set; } = default!;


        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // check if any of the debit or credit are source of import data
            FinAccountHelper fah = new(_context, default!);
            if(PlanTransaction.DebitAccount > 1000000 && PlanTransaction.CreditAccount > 1000000)
            {
                bool debitSource = await fah.AccountIsSource(PlanTransaction.DebitAccount);
                bool creditSource = await fah.AccountIsSource(PlanTransaction.CreditAccount);
                bool setErrorSource = false;
                if (debitSource && creditSource)
                {
                    if (PlanTransaction.IdSource != PlanTransaction.DebitAccount && PlanTransaction.IdSource != PlanTransaction.CreditAccount)
                    {
                        setErrorSource = true;
                    }
                }
                else if (debitSource)
                {
                    if (PlanTransaction.IdSource != PlanTransaction.DebitAccount)
                    {
                        setErrorSource = true;
                    }
                }
                else if (creditSource)
                {
                    if (PlanTransaction.IdSource != PlanTransaction.CreditAccount)
                    {
                        setErrorSource= true;
                    }
                }
                if(setErrorSource)
                {
                    ModelState.TryAddModelError("PlanTransaction.IdSource", "A valid source is required");
                }
            }
            if(PlanTransaction.TransactionDate < DateTime.Today)
            {
                ModelState.TryAddModelError("PlanTransaction.TransactionDate", "A date later or today is required");
            }
            if (!ModelState.IsValid)
            {
                FinAccList = await _cache.GetOrCreateAsync("FAL", () => GetFAL());
                return Page();
            }
            PlanTransaction.IdTransaction = GeneratorID.Int64IDBasedOnDate(PlanTransaction.TransactionDate.Date);
            _context.PlanTransactions.Add(PlanTransaction);
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Plan transaction with ID: {PlanTransaction.IdTransaction} successfully recorded!";
            }
            catch(DbException ex)
            {
                TempData["ErrorMessage"] = $"Ooops there was en error recording the data. \n {ex.Message}";
            }
            _cache.Remove("FAL");
            return RedirectToPage("FutureTransactions");
        }

        public async Task<SelectList> GetFAL()
        {
            return await _fillService.GetFinancialAccountsList();
        }
    }
}
