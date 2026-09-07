using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.EntityFrameworkCore;
using MySql.Data;
using MyFinances.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using MyFinances.Controllers;

namespace MyFinances.Pages
{
    public class IndexModel(ILogger<IndexModel> logger, AppDBContext dBContext) : PageModel
    {
        private readonly ILogger<IndexModel> _logger = logger;
        private readonly AppDBContext _dbContext = dBContext;

        public List<int> NrContractPerCateg = [];
        public List<string> ContTypesList = [];

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var cctl = await _dbContext.ContractTypes.OrderBy(p=>p.IdContractType).AsNoTracking().ToListAsync();
                if (cctl.Count > 0)
                {
                    var tnc = await _dbContext.FinContracts.OrderBy(p => p.ContractType).Select(s => s.ContractType).ToListAsync();
                    if (tnc.Count == 0) {
                        for(int i = 0;i<cctl.Count;i++)
                        {
                            NrContractPerCateg.Add(0);
                        }
                    }
                    else
                    {
                        int x = 0;
                        foreach (var c in cctl)
                        {
                            ContTypesList.Add(c.ContractTypeName);
                            x = tnc.Where(p=>p == c.IdContractType).Count();
                            NrContractPerCateg.Add(x);
                        }
                    }
                    
                }
            }
            catch(DbException dbex)
            {
                _logger.LogError("Error initializing the page {Error} at {Time}", dbex.ErrorCode, DateTime.Now);
                return RedirectPermanent("/Error");
            }
            return Page();
        }
    }
}
