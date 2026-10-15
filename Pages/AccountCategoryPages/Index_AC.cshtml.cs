using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;

namespace MyFinances.Pages.AccountCategoryPages
{
    public class Index_ACModel(AppDBContext appDbContext) : PageModel
    {
        private readonly AppDBContext _dbcontext = appDbContext;

        public List<AccCategNames> ACList = [];

        public async Task OnGet()
        {
            var acl = await _dbcontext.AccountCategories.Join(_dbcontext.AccountClasses, c => c.IdAccountClass, o => o.IdAccountClass, (c, o) => new { c.IdAccountCategory, o.AccountClassName, c.AccountCategoryName }).ToListAsync();
            if (acl.Count > 0)
            {
                foreach (var item in acl)
                {
                    ACList.Add(new AccCategNames() { IdAC = item.IdAccountCategory, AccClassName = item.AccountClassName, Name = item.AccountCategoryName });
                }
            }
        }

        public class AccCategNames
        {
            public int IdAC { set; get; } = 0;
            public string AccClassName { set; get; } = string.Empty;

            public string Name { set; get; } = string.Empty;
        }
    }
}
