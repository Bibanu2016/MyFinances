using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Pages.Organisations
{
    public class EditModel(MyFinances.Data.AppDBContext context) : PageModel
    {
        private readonly MyFinances.Data.AppDBContext _context = context;

        [BindProperty]
        public Organisation Organisation { get; set; } = default!;

        public string CategoryOrg { get; set; } = "Not assigned";

        public async Task<IActionResult> OnGetAsync(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organisation =  await _context.Organisations.FirstOrDefaultAsync(m => m.IdOrg == id);
            if (organisation == null)
            {
                return NotFound();
            }
            Organisation = organisation;
            var oc = await _context.OrganisationTypes.FindAsync(Organisation.IdCateg);
            if (oc != null)
            {
                CategoryOrg = oc.CategoryName;
            }
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Organisation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrganisationExists(Organisation.IdOrg))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./OrgIndex");
        }

        private bool OrganisationExists(int id)
        {
            return _context.Organisations.Any(e => e.IdOrg == id);
        }
    }
}
