using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Pages.FinTransactions
{
    public class DetailsPlanTransactionModel(AppDBContext context) : PageModel
    {
        private readonly AppDBContext _context = context;

        public PlanTransaction PlanTransaction { get; set; } = default!;
        public string ContractName { get; set; } = string.Empty;
        public string DebitName { get; set; } = string.Empty;
        public string CreditName { get; set; } = string.Empty;

        public string SourceName { get; set; } = string.Empty;

        public string DetailTransaction { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plantransaction = await _context.PlanTransactions.FirstOrDefaultAsync(m => m.IdTransaction == id);
            if (plantransaction == null)
            {
                return NotFound();
            }
            else
            {
                PlanTransaction = plantransaction;
                ViewData["Title"] = $"Details planned transaction id: {PlanTransaction.IdTransaction}";
                if (PlanTransaction.ContractIdentifier > 0)
                {
                    ContractName = await _context.FinContracts.Where(p => p.IdFinAccount == PlanTransaction.ContractIdentifier).Select(v => v.ContractName).FirstOrDefaultAsync() ?? string.Empty;
                }
                DebitName = await _context.FinAccounts.Where(p => p.IdFinAccount == PlanTransaction.DebitAccount).Select(v => v.Name).FirstOrDefaultAsync() ?? string.Empty;
                CreditName = await _context.FinAccounts.Where(p => p.IdFinAccount == PlanTransaction.CreditAccount).Select(v => v.Name).FirstOrDefaultAsync() ?? string.Empty;
                DetailTransaction = PlanTransaction.TransactionDetail + " " + ContractName;
                if (PlanTransaction.IdSource > 0)
                {
                    SourceName = await _context.FinAccounts.Where(p => p.IdFinAccount == PlanTransaction.IdSource).Select(v => v.Name).FirstOrDefaultAsync() ?? string.Empty;
                }
            }
            return Page();
        }
    }
}
