using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Pages.Accounts
{
    public class DeleteModel(MyFinances.Data.AppDBContext context, IConfiguration config, IMemoryCache mcache) : PageModel
    {
        private readonly MyFinances.Data.AppDBContext _context = context;
        private readonly IConfiguration _config = config;
        private readonly IMemoryCache _cache = mcache;

        [BindProperty]
        public FinAccount FinAccount { get; set; } = default!;
        [BindProperty]
        public bool CanDelete { get; set; } = true;
        public string NameCategory { get; set; } = string.Empty;
        public string NameOwner {  get; set; }  = string.Empty;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            FinAccount = await _context.FinAccounts.FirstOrDefaultAsync(m => m.IdFinAccount == id) ?? default!;
            if(FinAccount == default) { return NotFound(); }
            await FillOutNames();
            if(FinAccount.IsLinked) {CanDelete = false; ViewData["DelMessage"] = $"Account {FinAccount.Name} cannot be deleted as is part of a contract data"; }
            FinAccountHelper fah = new(_context, _config);
            StringBuilder sb = new();
            // user check
            bool cdfa = await fah.FinAccountUsedByUSer(FinAccount.IdFinAccount);
            if (cdfa)
            {
                ViewData["DelMessage"] = $"Account {FinAccount.Name} cannot be deleted as is part of a user data";
                CanDelete = false;
            }
            if(CanDelete && FinAccount.AccountCategory < 2000)
            {
                cdfa = await fah.FinAccountUsedByTAsset(FinAccount.IdFinAccount);
                if(!cdfa)
                {
                    cdfa = await fah.FinAccountUsedByShares(FinAccount.IdFinAccount);
                    if(cdfa)
                    {
                        CanDelete = false;
                        ViewData["DelMessage"] = $"Account {FinAccount.Name} cannot be deleted as is part of shares data";
                    }
                }
                else
                {
                    ViewData["DelMessage"] = $"Account {FinAccount.Name} cannot be deleted as is part of tangible asset data";
                    CanDelete = false;
                }
            }
            if(CanDelete)
            {
                cdfa = await fah.AccountIsUsedByTransactions(FinAccount.IdFinAccount);
                if(cdfa)
                {
                    CanDelete = false;
                    ViewData["DelMessage"] = $"Account {FinAccount.Name} cannot be deleted as is referenced in transactions";
                }
                
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _context.FinAccounts.Remove(FinAccount);
            try
            {
                int x = await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Account {FinAccount.Name} is deleted! \n {x} item deleted";
                _cache.Remove("FinAccountsList");
            }
            catch(DbException ex)
            {
                TempData["ErrorMessage"] = $"Ooops there was an error deleting {FinAccount.Name} \n {ex.Message}";
            }
            return RedirectToPage("./IndexFinAccount");
        }

        private async Task FillOutNames()
        {
            NameCategory = await _context.AccountCategories.AsNoTracking().Where(p=>p.IdAccountCategory == FinAccount.AccountCategory).Select(s=>s.AccountCategoryName).FirstOrDefaultAsync() ?? FinAccount.AccountCategory.ToString();
            NameOwner = await _context.UserContracts.AsNoTracking().Where(x => x.IdUser == FinAccount.AccountOwner).Select(s=>s.FullName).FirstOrDefaultAsync() ?? FinAccount.AccountOwner.ToString();

        }
    }
}
