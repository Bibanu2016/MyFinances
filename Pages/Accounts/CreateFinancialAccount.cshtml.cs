using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.ComponentModel;

namespace MyFinances.Pages.Accounts
{
    public class CreateFinancialAccountModel(AppDBContext context, IComboFillService fillService) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IComboFillService _fillService = fillService;
        [BindProperty] public FinAccount MyFinAccount { get; set; } = default!;

        public SelectList AccCategoryList { get; set; } = default!;
        public SelectList AccOwnerList { get; set; } = default!;
        [BindProperty] public long TID { set; get; } = 0;
        [BindProperty] public string TIFT { set; get; } = "";

        public async Task OnGet(long id = 0, string ift = "")
        {
            AccCategoryList = await _fillService.GetAllAccountCategoriesList();
            AccOwnerList = await _fillService.GetUsersList();
            MyFinAccount = new FinAccount()
            {
                IdFinAccount = 0
            };
            if(id > 0 && !string.IsNullOrEmpty(ift))
            {
                TID = id;
                TIFT = ift;
            }
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                AccCategoryList = await _fillService.GetAllAccountCategoriesList();
                AccOwnerList = await _fillService.GetUsersList();
                return Page();
            }
            FinAccountHelper fah = new(_context, default!);
            MyFinAccount.IdFinAccount = (await fah.NewIdAccountByCateg(MyFinAccount.AccountCategory))[0];
            MyFinAccount.IsLinked = false;
            _context.FinAccounts.Add(MyFinAccount);
            try
            {
                await _context.SaveChangesAsync();
                if (TID > 0 && !string.IsNullOrEmpty(TIFT))
                {
                    return RedirectToPage("/FinTransactions/EdittFinTransaction", new { id = TID, ift = TIFT });
                }
                else
                {
                    TempData["SuccessMessage"] = $"Financial account {MyFinAccount.Name} created successfully.";
                    return RedirectToPage("/IndexFinAccount");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Unable to save changes: {ex.Message}");
                AccCategoryList = await _fillService.GetAllAccountCategoriesList();
                AccOwnerList = await _fillService.GetUsersList();
                return Page();
            }
        }
    }
}
