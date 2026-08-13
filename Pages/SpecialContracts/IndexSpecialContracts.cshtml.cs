using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Data;
using MyFinances.Services;
using MyFinances.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Pages.SpecialContracts
{
    public class IndexSpecialContractsModel(AppDBContext context, IComboFillService fillService, IMemoryCache memory) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IComboFillService _fillService = fillService;
        private readonly IMemoryCache _memoryCache = memory;

        public List<ContractListVM> ContList { get; set; } = default!;
        public SelectList ContractTypes { get; set; } = default!;
        public SelectList UsersList { get; set; } = default!;

        [BindProperty, Required, Range(20, 99, ErrorMessage = "A valid selection is required")]
        public int CreateContractType { get; set; } = 0;
        public async Task<IActionResult> OnGet()
        {
            ContractTypes = await _fillService.GetSpecialContractTypes(20);
            UsersList = await _fillService.GetUsersList();
            if (ContractTypes == null || UsersList == null) { return NotFound(); }
            await PopulateContractList();
            return Page();
        }

        private async Task PopulateContractList()
        {
            var scl = await _context.FinContracts.Where(p => p.ContractType > 20 && p.IsActiveContract).ToListAsync();
            if (scl.Count > 0)
            {
                ContList = [];
                foreach (var contract in scl)
                {
                    ContList.Add(new ContractListVM()
                    {
                        IdFinContract = contract.IdFinContract,
                        ContractType = ContractTypes.Where(predicate => predicate.Value == contract.ContractType.ToString()).FirstOrDefault()?.Text ?? "not found",
                        ContractName = contract.ContractName,
                        OwnerName = UsersList.Where(predicate => predicate.Value == contract.IdContractor.ToString()).FirstOrDefault()?.Text ?? "not found",
                        EquivMonthlyRate = 0
                    });
                }
            }
            return;
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                ContractTypes = await _fillService.GetSpecialContractTypes(20);
                return Page();
            }
            string pagetogo = string.Empty;
            switch (CreateContractType)
            {
                case 21:
                    pagetogo = "./InvestFundPages/CreateIFContract";
                    break;
                case 22:
                    pagetogo = "./TaxationPages/CreateTaxContract";
                    break;
            }
            return RedirectToPage(pagetogo);
        }
    }
}
