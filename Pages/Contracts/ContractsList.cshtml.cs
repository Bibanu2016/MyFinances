using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.ViewModels;
using System.Text;

namespace MyFinances.Pages.Contracts
{
    public class ContractsListModel(AppDBContext context, IConfiguration configuration) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;

        public List<ContractListVM> ContractListVM { get; set; } = default!;
        public int CashFlowState { get; set; } = 0;
        public List<ContractType> ContractTypes { get; set; } = [];

        public async Task OnGetAsync()
        {
            var clvm = await _context.FinContracts.AsQueryable().Where(x => x.IsActiveContract).Select(y => new { y.IdFinContract, y.ContractType, y.ContractName, y.IdContractor, y.EquivMonthlyRate }).OrderBy(z => z.ContractType).ToListAsync();
            var ux = await _context.UserContracts.AsNoTracking().ToListAsync();
            var ctypel = await _context.ContractTypes.AsNoTracking().ToListAsync();
            if (ctypel.Count > 0)
            {
                foreach (var c in ctypel)
                {
                    if (c.IdContractType > 0 && c.IdContractType < 9) { ContractTypes.Add(c); } // do not add Taxation as it is added during user creation
                }
            }
            if (ux.Count > 0 && clvm.Count > 0 && ctypel.Count > 0)
            {
                ContractListVM = [];
                foreach (var c in clvm)
                {
                    ContractListVM clitem = new()
                    {
                        IdFinContract = c.IdFinContract,
                        ContractType = ctypel.FirstOrDefault(x => x.IdContractType == c.ContractType, new Models.ContractType()).ContractTypeName,
                        ContractName = c.ContractName,
                        OwnerName = ux.FirstOrDefault(y => y.IdUser == c.IdContractor, new Models.UserContract()).FullName,
                        EquivMonthlyRate = c.EquivMonthlyRate,
                    };
                    CashFlowState += c.EquivMonthlyRate;
                    ContractListVM.Add(clitem);
                }
            }
            return;
        }

        public async Task<IActionResult> OnPostEdit(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            var fc = await _context.FinContracts.Where(p => p.IdFinContract == id).Select(s => s.IdFinAccount).FirstOrDefaultAsync();
            if (fc < 1000000 || fc > 8000000) { return NotFound(); }
            int net = await _context.PlanTransactions.Where(p => p.ContractIdentifier == fc && p.ExecutedTransaction == false && p.TransactionDate < DateTime.Now).CountAsync();
            var fet = await _context.FinTransactions.Where(p => p.ContractIdentifier == fc).AsNoTracking().FirstOrDefaultAsync();
            if (net > 0 && fet != null) // contract has transations recorded and overdue plan transactions - no up to date
            {
                StringBuilder sb = new();
                sb.AppendLine($"Contract ID: {id} \n");
                sb.AppendLine($"has a total of {net} overdue transactions \n");
                sb.AppendLine($"as at {DateTime.Now} \n");
                sb.AppendLine("\n");
                sb.AppendLine("All contract transactions are required to be up-to-date in order to edit!");
                TempData["ErrorMessage"] = sb.ToString();
                return RedirectToPage();
            }
            return RedirectToPage("ContractEdit", new { pid = id });
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            var fc = await _context.FinContracts.Where(p => p.IdFinContract == id).Select(s => s.IdFinAccount).FirstOrDefaultAsync();
            if (fc < 1000000 || fc > 8000000) { return NotFound(); }
            var fet = await _context.FinTransactions.Where(p => p.ContractIdentifier == fc).AsNoTracking().FirstOrDefaultAsync();
            if (fet is not null)
            {
                // contract has recorded transactions - do not proceed if not all are executed
                int net = await _context.PlanTransactions.Where(p => p.ContractIdentifier == fc && p.ExecutedTransaction == false && p.TransactionDate < DateTime.Now).CountAsync();
                if (net > 0)
                {

                    StringBuilder sb = new();
                    sb.AppendLine($"Contract ID: {id} \n");
                    sb.AppendLine($"has a total of {net} overdue transactions \n");
                    sb.AppendLine($"as at {DateTime.Now} \n");
                    sb.AppendLine("\n");
                    sb.AppendLine("All contract transactions are required to be up-to-date in order to terminate!");
                    TempData["ErrorMessage"] = sb.ToString();
                    return RedirectToPage();
                }
            }
            // contract has no recorded transaction
            return RedirectToPage("ContractDelete", new { pid = id });
        }

    }
}
