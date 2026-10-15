using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;

namespace MyFinances.Pages.FinTransactions
{
    public class TransactionsFilter
    {
        public DateTime FromDate { get; set; } = DateTime.Now.AddMonths(-1);
        public DateTime ToDate { get; set; } = DateTime.Now;

        public int IdFilterAccount { get; set; } = -1;

        public int PageIndex { get; set; } = 1;

        public int PageSize { get; set; } = 20;
        public Dictionary<string, string?> ToRouteData(int page) => new()
        {
            [nameof(FromDate)] = FromDate.ToString("O"),
            [nameof(ToDate)] = ToDate.ToString("O"),
            [nameof(IdFilterAccount)] = IdFilterAccount.ToString(),
            [nameof(PageSize)] = PageSize.ToString(),
            [nameof(PageIndex)] = page.ToString(),
        };

        public override string ToString()
        {
            return FromDate.ToString("O") + "," + ToDate.ToString("O") + "," + IdFilterAccount.ToString() + "," + PageIndex.ToString() + "," + PageSize.ToString();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FromDate, ToDate, IdFilterAccount, PageSize);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is null || GetType() != obj.GetType()) return false;
            int result = 1;
            if (obj is TransactionsFilter other)
            {
                result = FromDate.Date == other.FromDate.Date ? result * 1 : 0;
                result = ToDate.Date == other.ToDate.Date ? result * 1 : 0;
                result = IdFilterAccount == other.IdFilterAccount ? result * 1 : 0;
                result = PageSize == other.PageSize ? result * 1 : 0;
            }
            return result == 1;
        }
    }
    public class IndexFinTransactionsModel(AppDBContext context, IConfiguration config, IComboFillService fillService, IMemoryCache cache) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _config = config;
        private readonly IComboFillService _fillService = fillService;
        private readonly IMemoryCache _cache = cache;

        public List<FinTransaction> FinTransactionList { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public TransactionsFilter ItemsFilter { get; set; } = new();
        public SelectList AccList { get; private set; } = default!;
        public int TotalCount { get; private set; }
        public int TotalPages { get; private set; }
        public IReadOnlyList<int> PageNumbers { get; private set; } = [];

        public int CurrentPage { get; set; } = 1;

        private static readonly int[] sourceArray = [10, 20, 30];

        public async Task OnGetAsync(string? rfe = null)
        {
            AccList = await _cache.GetOrCreateAsync("AList", () => GetAccountLists());
            bool checkReturn = false;
            TransactionsFilter temp = new();
            if (!string.IsNullOrEmpty(rfe))
            {
                string[] ifd = rfe.Split(',');
                if (ifd.Length == 5)
                {
                    if (DateTime.TryParse(ifd[0], out DateTime fd))
                    {
                        checkReturn = true;
                        temp.FromDate = fd;
                    }
                    if (DateTime.TryParse(ifd[1], out DateTime td))
                    {
                        checkReturn = checkReturn && true;
                        temp.ToDate = td;
                    }
                    if (int.TryParse(ifd[2], out int fa))
                    {
                        checkReturn = checkReturn && true;
                        temp.IdFilterAccount = fa;
                    }
                    if (int.TryParse(ifd[3], out int pidx))
                    {
                        checkReturn = checkReturn && true;
                        temp.PageIndex = pidx;
                    }
                    if (int.TryParse(ifd[4], out int psize))
                    {
                        checkReturn = checkReturn && true;
                        temp.PageSize = pidx;
                    }
                }
            }
            if (checkReturn) { ItemsFilter = temp; _cache.Remove("OIF"); }
            // ── Guard page size
            if (!sourceArray.Contains(ItemsFilter.PageSize)) ItemsFilter.PageSize = 20;
            if (ItemsFilter.PageIndex < 1) ItemsFilter.PageIndex = 1;
            // -- Count and paginate
            if (_cache.TryGetValue<TransactionsFilter>("OIF", out TransactionsFilter? oif))
            {
                if (!ItemsFilter.Equals(oif))
                {
                    _cache.Remove("TCount");
                    _cache.Remove("OIF");
                    _cache.Remove("Pages");
                }
            }
            TotalCount = await _cache.GetOrCreateAsync("TCount", () => ApplyFilters(_context.FinTransactions).CountAsync());
            TotalPages = (int)Math.Ceiling(TotalCount / (double)ItemsFilter.PageSize);
            if (ItemsFilter.PageIndex > TotalPages && TotalPages > 0) { ItemsFilter.PageIndex = TotalPages; }
            PageNumbers = _cache.GetOrCreate("Pages", () => BuildPageNumbers(ItemsFilter.PageIndex, TotalPages));
            FinTransactionList = await ApplyFilters(_context.FinTransactions).Skip((ItemsFilter.PageIndex - 1) * ItemsFilter.PageSize).Take(ItemsFilter.PageSize).ToListAsync();
            TransactionsFilter OrigIF = _cache.GetOrCreate("OIF", () => ItemsFilter);
        }

        private IQueryable<FinTransaction> ApplyFilters(IQueryable<FinTransaction> query)
        {
            query = query.Where(p => p.TransactionDate >= ItemsFilter.FromDate && p.TransactionDate < ItemsFilter.ToDate).OrderByDescending(y => y.TransactionDate).AsNoTracking();
            if (ItemsFilter.IdFilterAccount > -1)
            {
                query = query.Where(p => p.DebitAccount == ItemsFilter.IdFilterAccount || p.CreditAccount == ItemsFilter.IdFilterAccount).OrderByDescending(y => y.TransactionDate).AsNoTracking();
            }
            return query;
        }

        private static List<int> BuildPageNumbers(int current, int total)
        {
            if (total <= 1) return total == 1 ? [1] : [];
            const int wing = 2; // pages on each side of current
            var pages = new SortedSet<int>
            {
                // Always include first/last two and the window around current
                1,
                2,
                total - 1,
                total
            };
            for (int p = Math.Max(1, current - wing); p <= Math.Min(total,
            current + wing); p++)
                pages.Add(p);
            var result = new List<int>();
            int prev = 0;
            foreach (var p in pages)
            {
                if (p < 1 || p > total) continue;
                if (prev != 0 && p - prev > 1) result.Add(-1); // ellipsis
                result.Add(p);
                prev = p;
            }
            return result;
        }

        private async Task<SelectList> GetAccountLists()
        {
            return await _fillService.GetAccListFromTransactions();
        }


    }
}
