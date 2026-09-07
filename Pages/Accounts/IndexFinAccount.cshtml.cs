using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Models;
using MyFinances.ViewModels;

namespace MyFinances.Pages.Accounts
{
    public class IndexModel(MyFinances.Data.AppDBContext context, IMemoryCache cache) : PageModel
    {
        private readonly MyFinances.Data.AppDBContext _context = context;
        private readonly IMemoryCache _cache = cache;

        public List<FinAccount>? FinAccountsList { get;set; } = default!;

        public List<FinAccount> FilterredFAList { get;set; } = default!;
        public List<ComboIntStrLVM>? AccCategList { get;set; } = default!;
        public List<ComboIntStrLVM>? AccUserList { get;set; } = default!;

        public List<AccountCategory> AccountCategories { get;set; } = default!;

        public List<FinAccNames> FAList {  get;set; } = default!;


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
            AccCategList = await _cache.GetOrCreateAsync("AccCtgList", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                return await GetListAccCateg();
            });
            AccUserList = await _cache.GetOrCreateAsync("AccUserList", async entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                return await GetListUsers();
            });
            if(FinAccountsList != null)
            {
                FilterredFAList = [.. ApplyCategoryFilter(FinAccountsList.AsQueryable(), CategoryFilter)];
                FilterredFAList = [.. ApplyOwnerFilter(FilterredFAList.AsQueryable(), OwnerFilter)];
                FAList = FillAccountList();
            }
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

        private async Task<List<ComboIntStrLVM>> GetListAccCateg()
        {
            List<ComboIntStrLVM> list = [];
            if (FinAccountsList != null)
            {
                var result = await _context.FinAccounts.Join(_context.AccountCategories, c => c.AccountCategory, o => o.IdAccountCategory,
                    (c,o) => new { c.AccountCategory, o.AccountCategoryName}).AsNoTracking().ToListAsync();
                if (result.Count > 0)
                {
                    foreach (var item in result)
                    {
                        list.Add(new ComboIntStrLVM() { ItemID = item.AccountCategory, Name = item.AccountCategoryName });
                    }
                }
            }
            return [.. list.DistinctBy(x => x.ItemID)];
        }

        private async Task<List<ComboIntStrLVM>> GetListUsers()
        {
            List<ComboIntStrLVM> list = [];
            if (FinAccountsList != null)
            {
                var result = await _context.FinAccounts.Join(_context.UserContracts, c => c.AccountOwner, o => o.IdUser,
                    (c,o) => new { c.AccountOwner, o.FullName}).AsNoTracking().ToListAsync();
                if (result.Count > 0)
                {
                    foreach (var item in result)
                    {
                        list.Add(new ComboIntStrLVM() { ItemID = item.AccountOwner, Name = item.FullName });
                    }
                }
            }
            
            return [.. list.DistinctBy(x => x.ItemID)];
        }

        private List<FinAccNames> FillAccountList()
        {
            List<FinAccNames> list = [];
            if(FilterredFAList != null)
            {
                foreach (var account in FilterredFAList)
                {
                    FinAccNames fan = new()
                    {
                        Id = account.IdFinAccount,
                        Name = account.Name,
                    };
                    if (AccCategList != null) { fan.Category = AccCategList.Where(p => p.ItemID == account.AccountCategory).Select(s => s.Name).FirstOrDefault() ?? account.AccountCategory.ToString(); }
                    else { fan.Category = account.AccountCategory.ToString(); }
                    if(AccUserList != null)
                    {
                        fan.UserName = AccUserList.Where(p => p.ItemID == account.AccountOwner).Select(s => s.Name).FirstOrDefault() ?? account.AccountOwner.ToString();
                    }
                    else
                    {
                        account.AccountOwner.ToString();
                    }
                    list.Add(fan);
                }
            }
            return list;
        }
    }

    public class FinAccNames
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
