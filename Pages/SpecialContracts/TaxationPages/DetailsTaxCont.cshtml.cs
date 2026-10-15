using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Services;

namespace MyFinances.Pages.SpecialContracts.TaxationPages
{
    public class DetailsTaxContModel(AppDBContext context, IMyDetailsService detailsService) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IMyDetailsService _details = detailsService;

        public List<ViewModels.ItemDetail> ICDetails { set; get; } = [];

        public string ItemID { set; get; } = Guid.Empty.ToString();

        public async Task<IActionResult> OnGet(string id)
        {
            if (!Guid.TryParse(id, out _))
            {
                return NotFound();
            }
            ItemID = id;
            Guid cid = Guid.Parse(id);
            var mc = await _context.FinContracts.FindAsync(cid);
            if (mc is null)
            {
                return NotFound();
            }
            ViewData["Title"] = $"Details contract name : {mc.ContractName}";
            FinContractHelper fch = new(_context, default!);
            ICDetails = await fch.GetContractDetailsList(cid, _details);
            return Page();
        }
    }
}
