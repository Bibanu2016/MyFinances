using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using System.Data;
using System.Text.RegularExpressions;

namespace MyFinances.Pages.FinTransactions
{
    public class CreateTransacDictKeyModel(AppDBContext dBContext) : PageModel
    {
        private readonly AppDBContext _dbcontext = dBContext;
        [BindProperty]
        public TransacDictionary Tdictionary { get; set; } = new();
        public IList<int> AccCategories { get; set; } = [];
        public IList<ViewModels.ComboIntStrLVM> SourcesList { get; set; } = [];
        public IList<ViewModels.ComboIntStrLVM> TargetList { get; set; } = [];
        public async Task OnGet()
        {
            await FillOutCombos();
        }

        private async Task FillOutCombos()
        {
            // get the list with the account categories which can be used as source for the transactions importation
            var acl = await _dbcontext.AccountCategories.Select(v => new { v.IdAccountCategory, v.CategoryRoles }).AsNoTracking().ToArrayAsync();
            if (acl.Length > 0)
            {
                foreach (var c in acl)
                {
                    if (MyBitOperations.GetBit(c.CategoryRoles, 2))
                    {
                        AccCategories.Add(c.IdAccountCategory);
                    }
                }
            }
            var sol = await _dbcontext.FinAccounts.Where(p => p.AccountCategory >= 1000).Select(v => new { v.IdFinAccount, v.Name, v.AccountCategory }).ToListAsync();
            if (sol.Count > 0)
            {
                foreach (var sl in sol)
                {
                    TargetList.Add(new ViewModels.ComboIntStrLVM() { ItemID = sl.IdFinAccount, Name = sl.Name });
                    if (AccCategories.AsQueryable().Where(x => x == sl.AccountCategory).FirstOrDefault(0) != 0)
                    {
                        SourcesList.Add(new ViewModels.ComboIntStrLVM() { ItemID = sl.IdFinAccount, Name = sl.Name });
                    }
                }
            }
            return;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int count = Tdictionary.Name.Count(c => c == ' ');
            if (count > 1) { this.ModelState.TryAddModelError("Name", "Maximum two words are permited!"); }
            Regex rx = new(@"[^a-zA-Z0-9]");
            bool hasSpecial = rx.IsMatch(Tdictionary.Name);
            if (hasSpecial)
            {
                this.ModelState.TryAddModelError("Name", "No special characters are permited!");
            }
            if (!ModelState.IsValid)
            {
                await FillOutCombos();
                return Page();
            }
            Tdictionary.IdKey = GeneratorID.TransDictKeyId(Tdictionary.Name);
            _dbcontext.TransacDictionary.Add(Tdictionary);
            try
            {
                await _dbcontext.SaveChangesAsync();
            }
            catch (DBConcurrencyException ex)
            {
                TempData["ErrorMessage"] = $"An error occured recording the key. \n Please try again. \n {ex.Message}";
                await FillOutCombos();
                return Page();
            }
            return RedirectToPage("TransDictIndex");
        }

    }
}
