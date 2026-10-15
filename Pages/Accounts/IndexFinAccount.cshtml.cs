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

        public List<FinAccount>? FinAccountsList { get; set; } = default!;

        public List<FinAccount> FilterredFAList { get; set; } = default!;
        public List<FinAccountIndexViewModel> FAList { get; set; } = default!;

        public SelectList ACSL { get; set; } = default!;
        public SelectList OWSL { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public int CategoryFilter { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public int OwnerFilter { get; set; } = 0;

        public async Task OnGetAsync()
        {
            FinAccountsList = await _cache.GetOrCreateAsync("FinAccountsList", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                return await _context.FinAccounts.OrderBy(p => p.IdFinAccount).AsNoTracking().ToListAsync(); // DB or API call
            });
            if (FinAccountsList != null)
            {
                FilterredFAList = [.. ApplyCategoryFilter(FinAccountsList.AsQueryable(), CategoryFilter)];
                FilterredFAList = [.. ApplyOwnerFilter(FilterredFAList.AsQueryable(), OwnerFilter)];
            }
            await FillOutLists(FilterredFAList);
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

        private async Task FillOutLists(List<FinAccount> finAccounts)
        {
            if (finAccounts != null)
            {
                if (finAccounts.Count > 0)
                {
                    var acl = await _context.AccountCategories.Where(p => p.IdAccountCategory > 0).AsNoTracking().ToListAsync();
                    var jacl = finAccounts.LeftJoin(acl, fa => fa.AccountCategory, ac => ac.IdAccountCategory, (fa, ac) => new { fa.IdFinAccount, fa.AccountCategory, ac.AccountCategoryName }).ToList();
                    var ual = await _context.UserContracts.Where(p => p.IdUser > 0).AsNoTracking().ToListAsync();
                    var jual = finAccounts.LeftJoin(ual, fa => fa.AccountOwner, uc => uc.IdUser, (fa, uc) => new { fa.IdFinAccount, fa.AccountOwner, uc.FullName }).ToList();
                    var dja = jacl.DistinctBy(p => p.AccountCategory).ToList();
                    var dju = jual.DistinctBy(p => p.AccountOwner).ToList();
                    ACSL = new SelectList(dja, nameof(FinAccount.AccountCategory), nameof(AccountCategory.AccountCategoryName));
                    OWSL = new SelectList(dju, nameof(FinAccount.AccountOwner), nameof(UserContract.FullName));
                    FAList = new List<FinAccountIndexViewModel>();
                    foreach (var fa in finAccounts)
                    {
                        var ac = jacl.FirstOrDefault(p => p.AccountCategory == fa.AccountCategory);
                        var uc = jual.FirstOrDefault(p => p.IdFinAccount == fa.IdFinAccount);
                        if (ac != null && uc != null)
                        {
                            var vm = new FinAccountIndexViewModel
                            {
                                IdFinAccount = fa.IdFinAccount,
                                FAName = fa.Name,
                                FACategory = ac.AccountCategoryName,
                                FAUser = uc.FullName,
                                FALinked = (byte)(fa.IsLinked ? 1 : 0)
                            };
                            FAList.Add(vm);
                        }
                    }
                }
            }
            return;
        }
    }

    public class FinAccountIndexViewModel
    {
        public int IdFinAccount { get; set; } = default!;
        public string FAName { get; set; } = default!;
        public string FACategory { get; set; } = default!;
        public string FAUser { get; set; } = default!;
        public byte FALinked { get; set; } = default!;
    }
}
