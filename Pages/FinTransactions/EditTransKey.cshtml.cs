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
using MyFinances.Services;

namespace MyFinances.Pages.FinTransactions
{
    public class EditTransKeyModel(MyFinances.Data.AppDBContext context, IComboFillService fillService) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IComboFillService _fillService = fillService;

        [BindProperty]
        public TransacDictionary TransacDictionary { get; set; } = default!;

        public SelectList FinAccList { get; set; } = default!;
        public SelectList SourcesList { get; set; } = default!;

        public string FARedirect { get; set; } = string.Empty;


        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            FARedirect = "/FinTransactions/EditTransKey," + id.ToString();
            var transacdictionary =  await _context.TransacDictionary.FirstOrDefaultAsync(m => m.IdKey == id);
            if (transacdictionary == null)
            {
                return NotFound();
            }
            TransacDictionary = transacdictionary;
            FinAccList = await _fillService.GetFinancialAccountsList();
            SourcesList = await _fillService.GetSourcesListForTransacKeys();
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (await TransacDictionaryExists(TransacDictionary)) { ModelState.TryAddModelError("Name", errorMessage: "This structure already exists!"); }
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(TransacDictionary).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dex)
            {
                TempData["ErrorMessage"] = $"Ooops and error occured! \n {dex.Message}";
            }

            return RedirectToPage("./TransDictIndex");
        }

        private async Task<bool> TransacDictionaryExists(TransacDictionary td)
        {
            return await _context.TransacDictionary.Where(e => e.IdSource == td.IdSource && e.Name == td.Name && e.IdTarget == td.IdTarget).FirstOrDefaultAsync() != null;
        }
    }
}
