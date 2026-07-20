using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Models;
using MyFinances.Data;

namespace MyFinances.Pages.AccountCategoryPages;

public class CreateModel : PageModel
{
    private readonly AppDBContext _context;

    public CreateModel(AppDBContext context)
    {
        _context = context;
    }

    public IActionResult OnGet()
    {
        AccountCategory.CategoryRoles = [0b_0000_0000];
        return Page();
    }

    [BindProperty]
    public AccountCategory AccountCategory { get; set; } = default!;

    // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        
        if (!ModelState.IsValid)
        {
            return Page();
        }  
        _context.AccountCategories.Add(AccountCategory);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
