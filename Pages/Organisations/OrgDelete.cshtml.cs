using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Pages.Organisations
{
    public class DeleteModel : PageModel
    {
        private readonly MyFinances.Data.AppDBContext _context;

        public DeleteModel(MyFinances.Data.AppDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Organisation Organisation { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organisation = await _context.Organisations.FirstOrDefaultAsync(m => m.IdOrg == id);

            if (organisation == null)
            {
                return NotFound();
            }
            else
            {
                Organisation = organisation;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organisation = await _context.Organisations.FindAsync(id);
            if (organisation != null)
            {
                Organisation = organisation;
                _context.Organisations.Remove(Organisation);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./OrgIndex");
        }
    }
}
