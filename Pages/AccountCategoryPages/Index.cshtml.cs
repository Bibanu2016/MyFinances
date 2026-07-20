using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Models;
using MyFinances.Data;

namespace MyFinances.Pages.AccountCategoryPages;

public class IndexModel : PageModel
{
    private readonly AppDBContext _context;

    public IndexModel(AppDBContext context)
    {
        _context = context;
    }

    public IList<AccountCategory> AccountCategory { get; set; } = default!;

    public async Task OnGetAsync()
    {
        AccountCategory = await _context.AccountCategories.ToListAsync();
    }
}
