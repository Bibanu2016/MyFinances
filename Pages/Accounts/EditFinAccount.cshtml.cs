using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using System.Data.Common;

namespace MyFinances.Pages.Accounts
{
    public class EditModel(AppDBContext context, IConfiguration config) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _config = config;

        [BindProperty]
        public FinAccount FinAccount { get; set; } = default!;

        public string CategoryAccount { get; set; } = string.Empty;
        public string NameOwner { get; set; } = string.Empty;
        [BindProperty]
        public short CanEdit { get; set; } = 0;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var finaccount = await _context.FinAccounts.FirstOrDefaultAsync(m => m.IdFinAccount == id);
            if (finaccount == null)
            {
                return NotFound();
            }
            FinAccount = finaccount;
            await FillCombos();
            FinAccountHelper fah = new(_context, _config);
            bool isused = await fah.AccountUsedInContract(FinAccount.IdFinAccount);
            CanEdit = isused ? (short)0 : (short)1;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await FillCombos();
                return Page();
            }
            _context.Attach(FinAccount).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbException ex)
            {
                TempData["ErrorMessage"] = $"Account {FinAccount.Name} failed to record to database. \n {ex.Message}";
            }
            return RedirectToPage("./IndexFinAccount");
        }

        private bool FinAccountExists(int id)
        {
            return _context.FinAccounts.Any(e => e.IdFinAccount == id);
        }

        private async Task FillCombos()
        {
            var x = await _context.AccountCategories.AsNoTracking().FirstOrDefaultAsync(x => x.IdAccountCategory == FinAccount.AccountCategory);
            var y = await _context.UserContracts.AsNoTracking().FirstOrDefaultAsync(x => x.IdUser == FinAccount.AccountOwner);
            if (x != null) { CategoryAccount = x.AccountCategoryName; }
            if (y != null) { NameOwner = y.FullName; }
            return;
        }
    }
}
