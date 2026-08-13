using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.Data.Common;

namespace MyFinances.Pages.SpecialContracts.TaxationPages
{
    public class EditTaxContModel(AppDBContext context, IConfiguration configuration, IMemoryCache memoryCache, IComboFillService comboFill) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly IComboFillService _comboFill = comboFill;

        // Combobox source data
        public SelectList Frequency { get; set; } = default!;
        public SelectList Organisation { get; set; } = default!;
        public SelectList Users { get; set; } = default!;
        // Contract data
        [BindProperty]
        public FinContract MainContract { get; set; } = default!;
        [BindProperty]
        public bool IsExecuted { get; set; } = false;

        public async Task<IActionResult> OnGet(int id)
        {
            // Load the contract data from the database
            var mc = await _context.FinContracts.FindAsync(id);
            if (mc == null) { return NotFound(); }
            MainContract = mc;
            FinContractHelper fch = new(_context, _configuration);
            IsExecuted = await fch.GetContractExecutionState(MainContract.IdFinAccount);
            // Load combobox data
            await FillUpLists();
            ViewData["Title"] = $"Edit Taxation Contract {MainContract.ContractName}";
            return Page();
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
            _context.FinContracts.Update(MainContract);
            try
            {
                int i = await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Contract {MainContract.ContractName} updated successfully \n {i} records affected.";
            }
            catch(DbException dex)
            {
                TempData["ErrorMessage"] = $"Ooops .. there was an error with database operations! \n {dex.Message}";
            }
            return RedirectToPage("../IndexSpecialContracts");
        }

    }
}
