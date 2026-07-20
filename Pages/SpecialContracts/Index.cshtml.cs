using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyFinances.Data;
using MyFinances.Services;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Pages.SpecialContracts
{
    public class IndexModel(AppDBContext context, IComboFillService fillService) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IComboFillService _fillService = fillService;

        public SelectList ContractTypes { get; set; } = default!;

        [BindProperty, Required, Range(20, 99, ErrorMessage = "A valid selection is required")]
        public int CreateContractType { get; set; } = 0;


        public async void OnGet()
        {
            ContractTypes = await _fillService.GetSpecialContractTypes(20);
        }

        public async Task<IActionResult> OnPost()
        {
            return RedirectToPage("/SpecialContracts/InvestFundPages/InvestFundContractAdd");
        }
    }
}
