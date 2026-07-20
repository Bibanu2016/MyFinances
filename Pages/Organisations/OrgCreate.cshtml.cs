using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Pages.Organisations
{
    public class CreateModel(AppDBContext context, IConfiguration configuration) : PageModel
    {
        private readonly MyFinances.Data.AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;

        public List<OrganisationType> OrganisationTypes { get; set; } = [];
        [BindProperty (SupportsGet = true)]
        public bool FromContract { set; get; } = false;
        public int Ctype { get; set; } = 0;

        public async Task<IActionResult> OnGet(bool fc, int ct)
        {
            FromContract = fc;
            Ctype = ct;
            var ots = await _context.OrganisationTypes.ToListAsync();
            if(ots.Count > 0)
            {
                OrganisationTypes = ots;
            }
            return Page();
        }

        [BindProperty]
        public Organisation Organisation { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            GeneralHelper gh = new(_context, _configuration);
            Organisation.IdOrg = await gh.GetOrganisationId(Organisation.IdCateg);
            Organisation.NumberContracts = 0;
            _context.Organisations.Add(Organisation);
            await _context.SaveChangesAsync();
            if(FromContract)
            {
                if(Ctype < 8)
                {
                    return RedirectToPage("/Contracts/ContractAdd", Ctype);
                }
                else
                {
                    return RedirectToPage("/Contracts/InvestFundContractAdd");
                }               
            }
            else
            {
                return RedirectToPage("./OrgIndex");
            }
        }
    }
}
