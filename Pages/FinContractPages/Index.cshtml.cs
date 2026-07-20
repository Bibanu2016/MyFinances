using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Models;
using MyFinances.Data;

namespace MyFinances.Pages.FinContractPages;

public class IndexModel : PageModel
{
    private readonly AppDBContext _context;

    public IndexModel(AppDBContext context)
    {
        _context = context;
    }

    public IList<FinContract> FinContract { get; set; } = default!;

    public async Task OnGetAsync()
    {
        FinContract = await _context.FinContracts.ToListAsync();
    }
}
