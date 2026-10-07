using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;

namespace MyFinances.Pages.Accounts
{
    public class EditModel(AppDBContext context, IConfiguration config, IComboFillService comboFill, IMemoryCache cache) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _config = config;
        private readonly IComboFillService _comboFill = comboFill;
        private readonly IMemoryCache _cache = cache;

        [BindProperty]
        public FinAccount FinAccount { get; set; } = default!;

        public string CategoryAccount {  get; set; } = string.Empty;
        public string NameOwner {  get; set; } = string.Empty;
        public SelectList AccCategList { get; set; } = default!;
        public SelectList UserList { get; set; } = default!;
        [BindProperty]
        public short CanEdit { get; set; } = 0;

        private FinAccount OriginalAccount { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var finaccount =  await _context.FinAccounts.FirstOrDefaultAsync(m => m.IdFinAccount == id);
            if (finaccount == null)
            {
                return NotFound();
            }
            FinAccount = finaccount;
            await FillCombos();
            FinAccountHelper fah = new(_context, _config);
            bool isused = await fah.AccountIsUsed(FinAccount.IdFinAccount);
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
            var original = await _context.FinAccounts.AsNoTracking().FirstOrDefaultAsync(m => m.IdFinAccount == FinAccount.IdFinAccount);
            if (original != null)
            {
                OriginalAccount = original;
                if (OriginalAccount.AccountCategory != FinAccount.AccountCategory)
                {
                    // get new account id
                    FinAccountHelper helper = new(_context, _config);
                    int nfa = (await helper.NewIdAccountByCateg(FinAccount.AccountCategory))[0];
                    FinAccount NewFa = new()
                    {
                        IdFinAccount = nfa,
                        Name = FinAccount.Name,
                        AccountCategory = FinAccount.AccountCategory,
                        AccountOwner = FinAccount.AccountOwner,
                        IsLinked = FinAccount.IsLinked
                    };
                    _context.FinAccounts.Add(NewFa);
                    _context.FinAccounts.Remove(FinAccount);
                }
                else
                {
                    _context.Attach(FinAccount).State = EntityState.Modified;
                }
            }
            try
            {
                await _context.SaveChangesAsync();
                _cache.Remove("FinAccountsList");
            }
            catch (DbException ex)
            {
                TempData["ErrorMessage"] = $"Account {FinAccount.Name} failed to record to database. \n {ex.Message}";
            }
            return RedirectToPage("./IndexFinAccount");
        }


        private async Task FillCombos()
        {
            AccCategList = await _comboFill.GetAllAccountCategoriesList();
            UserList = await _comboFill.GetUsersList();
            return;
        }
    }
}
