using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Models;
using MyFinances.Data;

namespace MyFinances.Pages.PlanTransactionPages;

public class DeleteModel : PageModel
{
    private readonly AppDBContext _context;

    public DeleteModel(AppDBContext context)
    {
        _context = context;
    }

    [BindProperty]
    public PlanTransaction PlanTransaction { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(long? idtransaction)
    {
        if (idtransaction is null)
        {
            return NotFound();
        }

        var plantransaction = await _context.PlanTransactions.FirstOrDefaultAsync(m => m.IdTransaction == idtransaction);
        if (plantransaction is null)
        {
            return NotFound();
        }
        else
        {
            PlanTransaction = plantransaction;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long? idtransaction)
    {
        if (idtransaction is null)
        {
            return NotFound();
        }

        var plantransaction = await _context.PlanTransactions.FindAsync(idtransaction);
        if (plantransaction != null)
        {
            PlanTransaction = plantransaction;
            _context.PlanTransactions.Remove(PlanTransaction);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
