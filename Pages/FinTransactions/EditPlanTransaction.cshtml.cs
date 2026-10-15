using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.Data.Common;

namespace MyFinances.Pages.FinTransactions
{
    public class EditPlanTransactionModel(AppDBContext context, IConfiguration myconfig, IComboFillService comboFill) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _myconfig = myconfig;
        private readonly IComboFillService _comboFill = comboFill;

        [BindProperty]
        public PlanTransaction MyPlanTransaction { get; set; } = default!;

        public SelectList AccList { get; set; } = default!;
        [BindProperty]
        public bool IsPastTransac { get; set; } = false;
        [BindProperty]
        public int DeltaPlanRecord { get; set; } = 0;

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var x = await _context.PlanTransactions.FindAsync(id);
            if (x is null)
            {
                return NotFound();
            }
            MyPlanTransaction = x;
            ViewData["Title"] = $"Edit planned transaction ID= {MyPlanTransaction.IdTransaction}";
            AccList = await _comboFill.GetFinancialAccountsList();
            IsPastTransac = MyPlanTransaction.TransactionDate < DateTime.Now;
            if (IsPastTransac)
            {
                DeltaPlanRecord = await DeltaOnExecutedTransactions(MyPlanTransaction);
            }
            //planned state
            //-- Contract id > 0 NO Edit
            //-- Contract id == 0 Edit ALL - check is still Future
            //overdue state
            //-- Contract id > 0 Edit only executed status
            //--Contract id == 0 Edit Amount and Executed status
            //executed state
            //--Contract id > 0 Edit only executed status
            //--Contract id == 0 Edit only executed status
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (IsPastTransac)
            {
                // get the original execution status
                var oes = await _context.PlanTransactions.Where(p => p.IdTransaction == MyPlanTransaction.IdTransaction).Select(s => s.ExecutedTransaction).FirstOrDefaultAsync();
                if (MyPlanTransaction.ExecutedTransaction != oes)
                {
                    _context.PlanTransactions.Update(MyPlanTransaction);
                    try
                    {
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"Transaction id={MyPlanTransaction.IdTransaction} execution status successfully changed";
                    }
                    catch (DbException ex)
                    {
                        TempData["ErrorMessage"] = $"Ooops there was an error in processing this transaction \n {ex.Message}";
                    }

                }
            }
            else
            {
                if (MyPlanTransaction.ContractIdentifier == 0)
                {
                    if (MyPlanTransaction.TransactionDate < DateTime.Today)
                    {
                        ModelState.TryAddModelError("MyPlanTransaction.TransactionDate", $"A date greater than or equal to {DateTime.Today} is required");
                    }
                    if (!ModelState.IsValid)
                    {
                        AccList = await _comboFill.GetFinancialAccountsList();
                        return Page();
                    }
                    _context.PlanTransactions.Update(MyPlanTransaction);
                    try
                    {
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"Transaction id={MyPlanTransaction.IdTransaction} successfully changed";
                    }
                    catch (DbException ex)
                    {
                        TempData["ErrorMessage"] = $"Ooops there was an error in processing this transaction \n {ex.Message}";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = $"No changes made to transaction id={MyPlanTransaction.IdTransaction}";
                }
            }
            return RedirectToPage("./FutureTransactions");
        }

        private async Task<int> DeltaOnExecutedTransactions(PlanTransaction pt)
        {
            // get exect transactions to date of pt
            int trt = await _context.FinTransactions.Where(p => p.TransactionDate <= pt.TransactionDate && p.DebitAccount == pt.DebitAccount && p.CreditAccount == pt.CreditAccount && p.IdSource == pt.IdSource).CountAsync();
            // get the exec plantransactions before pt inclusive
            int tpt = await _context.PlanTransactions.Where(p => p.TransactionDate <= pt.TransactionDate && p.DebitAccount == pt.DebitAccount && p.CreditAccount == pt.CreditAccount && p.IdSource == pt.IdSource).CountAsync();
            return trt - tpt; //executed - planed
        }
    }
}
