using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Models;

namespace MyFinances.Pages.Accounts
{
    public class IndexModel(MyFinances.Data.AppDBContext context, IMemoryCache cache) : PageModel
    {
        private readonly MyFinances.Data.AppDBContext _context = context;
        private readonly IMemoryCache _cache = cache;

        public List<FinAccount>? FinAccountsList { get;set; } = default!;

        public List<FinAccount> FilterredFAList { get;set; } = default!;

        public List<AccountCategory> AccountCategories { get;set; } = default!;
        public List<UserContract> UserContracts { get;set; } = default!;

        public SelectList ACSL { get;set; } = default!;
        public SelectList OWSL { get; set; } = default!;

        [BindProperty(SupportsGet =true)]
        public int CategoryFilter {  get; set; } = 0;

        [BindProperty(SupportsGet =true)]
        public int OwnerFilter { get; set; } = 0;

        public async Task OnGetAsync()
        {
            FinAccountsList = await _cache.GetOrCreateAsync("FinAccountsList", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                return await _context.FinAccounts.OrderBy(p => p.IdFinAccount).AsNoTracking().ToListAsync(); // DB or API call
            });
            if(FinAccountsList != null)
            {
                FilterredFAList = [.. ApplyCategoryFilter(FinAccountsList.AsQueryable(), CategoryFilter)];
                FilterredFAList = [.. ApplyOwnerFilter(FilterredFAList.AsQueryable(), OwnerFilter)];
            }
            
            AccountCategories = await _context.AccountCategories.Where(p=>p.IdAccountCategory > 0).AsNoTracking().ToListAsync();
            ACSL = new SelectList(AccountCategories, nameof(AccountCategory.IdAccountCategory), nameof(AccountCategory.AccountCategoryName));
            UserContracts = await _context.UserContracts.Where(p=>p.IdUser > 0).AsNoTracking().ToListAsync();
            OWSL = new SelectList(UserContracts, nameof(UserContract.IdUser), nameof(UserContract.FullName));
        }

        private async Task<List<FinAccount>> GetFinAccountsList()
        {
            return await _context.FinAccounts.OrderBy(p => p.IdFinAccount).AsNoTracking().ToListAsync();
        }

        private static IQueryable<FinAccount> ApplyCategoryFilter(IQueryable<FinAccount> queryCategory, int categID)
        {
            if (categID > 0)
            {
                return queryCategory = queryCategory.Where(p => p.AccountCategory == categID).AsNoTracking();
            }
            else
            {
                return queryCategory = queryCategory.AsNoTracking();
            }
            
        }

        private static IQueryable<FinAccount> ApplyOwnerFilter(IQueryable<FinAccount> queryOwner, int onwerID)
        {
            if (onwerID > 0)
            {
                return queryOwner = queryOwner.Where(p => p.AccountOwner == onwerID).AsNoTracking();
            }
            else
            {
                return queryOwner = queryOwner.AsNoTracking();
            }
            
        }
    }
}
