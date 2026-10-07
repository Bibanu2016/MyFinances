using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.Data.Common;

namespace MyFinances.Pages.SpecialContracts.TaxationPages
{
    public class CreateTaxContractModel(AppDBContext context, IConfiguration configuration, IMemoryCache memoryCache, IComboFillService comboFill) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly IComboFillService _comboFill = comboFill;

        [BindProperty] public int CategoryContract { get; set; } = 5005;


        // Combobox source data
        public SelectList Frequency { get; set; } = default!;
        public SelectList Organisation { get; set; } = default!;
        public SelectList Users { get; set; } = default!;
        // Contract data
        [BindProperty]
        public FinContract MainContract { get; set; } = default!;

        public async Task OnGetAsync()
        {
            short cct = _configuration.GetSection("AccountCategoriesSettings").GetValue<short>("TaxPaid");
            if (cct == 0) { CategoryContract = 5005; } else { CategoryContract = cct; }
            ViewData["Title"] = "Create Taxation Contract";
            await FillUpLists();

        }

        // --- Procedures to create the lists ---
        /// <summary>
        /// Populates the lists with the data from the database
        /// </summary>
        /// <returns></returns>
        private async Task FillUpLists()
        {
            Frequency = await _comboFill.GetPaymentFrequencyList();
            Organisation = await _comboFill.GetOrganisationsList(19);
            //consider addin the state and local organisations to the list of organisations
            Users = await _comboFill.GetUsersList();
            return;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await FillUpLists();
                return Page();
            }
            MainContract.IdFinContract = new Guid();
            var ctype = _configuration.GetSection("ContractTypes").GetValue<short>("Taxation");
            MainContract.ContractType = ctype > 0 ? ctype : (short)22;
            _context.FinContracts.Add(MainContract);        
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Contract {MainContract.ContractName} created successfully.";
            }
            catch (DbException dex)
            {
                TempData["ErrorMessage"] = $"Database error: {dex.Message}";
            }
            return RedirectToPage("../IndexSpecialContracts");
        }


    }
}
