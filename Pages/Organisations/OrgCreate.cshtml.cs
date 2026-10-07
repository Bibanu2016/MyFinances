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
            Organisation.IdOrg = await GetOrganisationId(Organisation.IdCateg);
            Organisation.NumberContracts = 0;
            _context.Organisations.Add(Organisation);
            await _context.SaveChangesAsync();
            if(FromContract)
            {
                if(Ctype < 8)
                {
                    return RedirectToPage("/Contracts/ContractAdd", Ctype);
                }
                if(Ctype == _configuration.GetSection("ContractTypes").GetValue<int>("InvestFundContract"))
                {
                    return RedirectToPage("/SpecialContracts/CreateIFContract");
                }
                if (Ctype == _configuration.GetSection("ContractTypes").GetValue<int>("Taxation"))
                {
                    return RedirectToPage("/SpecialContracts/TaxationPages");
                }
            }
            return RedirectToPage("./OrgIndex");
        }

        private async Task<int> GetOrganisationId(short categoryId)
        {
            int result = 0;
            var losc = await _context.Organisations.Where(x => x.IdCateg == categoryId).OrderBy(y => y.IdOrg).AsNoTracking().ToListAsync();
            if (losc.Count > 0)
            {
                int idx = categoryId * 1000;
                for (int i = 0; i < losc.Count; i++)
                {
                    if (losc[i].IdOrg > (idx + 1))
                    {
                        result = idx + 1; break;
                    }
                    idx++;
                }
                if (result == 0) { result = categoryId * 1000 + idx; }
            }
            else
            {
                result = categoryId * 1000 + 1;
            }
            return result;
        }
    }
}
