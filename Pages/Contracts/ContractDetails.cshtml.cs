using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceCalculator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using MyFinances.ViewModels;

namespace MyFinances.Pages.Contracts
{
    public class ContractDetailsModel(AppDBContext context, IMyDetailsService detailsService, IConfiguration configuration) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IMyDetailsService _myDetailsService = detailsService;
        private readonly IConfiguration _configuration = configuration;

        public List<ItemDetail> ItemDetails { private set; get; } = [];

        public Guid ItemId { private set; get; } = Guid.Empty;

        public ContractFull Full { get; private set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            Guid gid = id ?? Guid.Empty;
            ItemId = gid; 
            FinContractHelper fch = new(_context, _configuration);
            ItemDetails = await fch.GetContractDetailsList(gid, _myDetailsService);
            if(ItemDetails.Count > 1)
            {
                ViewData["Title"] = $"Details contract: {ItemDetails[1]?.ItemValue}";
            }
            ViewData["Title"] = $"Details contract: no name";
            int idFA = await _context.FinContracts.Where(p => p.IdFinContract == gid).Select(s => s.IdFinAccount).FirstOrDefaultAsync();
            if(idFA > 1000000) 
            {
                // get start FY
                GeneralHelper gh = new(_configuration);
                DateTime startFY = gh.StartFinancialYear(DateTime.Today);
                // get executed transactions dates
                var etdList = await _context.FinTransactions.Where(p => p.ContractIdentifier == idFA && p.TransactionDate <= DateTime.Today).Select(s => s.TransactionDate).Distinct().Order().ToListAsync();
                if(etdList.Count > 0)
                {
                    ItemDetails.Add(new ItemDetail() { ItemName = "Last transaction date", ItemValue = etdList.Max().ToShortDateString() });
                    if(idFA < 4000000)
                    {
                        var sb = await _context.FinTransactions.Where(p => p.ContractIdentifier == idFA && p.TransactionDate == etdList[0]).Select(s => s.TransactionValue).FirstOrDefaultAsync();
                        ItemDetails.Add(new ItemDetail() { ItemName = $"Start balance at {etdList[0].ToShortDateString()}", ItemValue = sb.ToString("C", default) });
                    }

                }
                else
                {
                    ItemDetails.Add(new ItemDetail() { ItemName = "Has recorded transactions", ItemValue = "No recorded transactions" });
                }
            }
            return Page();
        }
    }
}
