using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Models;

namespace MyFinances.Pages.Organisations
{
    public class DetailsModel(MyFinances.Data.AppDBContext context) : PageModel
    {
        private readonly MyFinances.Data.AppDBContext _context = context;
        public string NameCategory = "Not assigned";

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
                var oc = await _context.OrganisationTypes.FindAsync(id);
                if (oc != null) { NameCategory = oc.CategoryName; }
            }
            return Page();
        }
    }
}
