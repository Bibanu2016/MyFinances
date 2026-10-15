using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Pages.FinTransactions
{
    public class EditFinTransactionModel(AppDBContext dBContext, IComboFillService fillService, IMyDetailsService detailsService) : PageModel
    {
        private readonly AppDBContext _dbContext = dBContext;
        private readonly IComboFillService _fillService = fillService;
        private readonly IMyDetailsService _myDetailsService = detailsService;

        [BindProperty]
        public FinTransaction EditingFT { get; set; } = default!;
        [BindProperty, Required, MaxLength(45, ErrorMessage = "A lenght of less than 45 char is required"), MinLength(3, ErrorMessage = "A minimum length of 3 char is required")]
        public string AccountKey { get; set; } = string.Empty;
        public string NameContract { get; set; } = string.Empty;

        [BindProperty]
        public short IsCreditTheSource { get; set; } = -1;
        [BindProperty]
        public string ReturnRoute { get; set; } = string.Empty;

        public SelectList FAList { get; set; } = default!;
        public async Task<IActionResult> OnGet(long id, string ift, int? accnew)
        {
            var eft = await _dbContext.FinTransactions.FindAsync(id);
            if (eft == null) return NotFound();
            EditingFT = eft;
            if (!string.IsNullOrEmpty(ift))
            {
                ReturnRoute = ift;
            }
            if (accnew.HasValue)
            {
                if (EditingFT.DebitAccount == 0) { EditingFT.DebitAccount = accnew ?? 0; }
                if (EditingFT.CreditAccount == 0) { EditingFT.CreditAccount = accnew ?? 0; }
            }
            ViewData["Title"] = $"Edit Transaction ID: {EditingFT.IdTransaction}";
            TempData["OrigTD"] = EditingFT.TransactionDetail;
            if (EditingFT.DebitAccount == EditingFT.IdSource) { IsCreditTheSource = 0; }
            if (EditingFT.CreditAccount == EditingFT.IdSource) { IsCreditTheSource = 1; }
            FAList = await _fillService.GetFinancialAccountsList();
            NameContract = await _myDetailsService.GetAccountNameByID(EditingFT.ContractIdentifier);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            switch (IsCreditTheSource)
            {
                case -1:
                    ModelState.Remove(nameof(AccountKey)); // no account key or if one exists change target and source in the TransDictionary
                    break;
                case 0:
                    if (EditingFT.CreditAccount == 0) { ModelState.TryAddModelError(nameof(EditingFT.CreditAccount), errorMessage: "A values is required"); }
                    break;
                case 1:
                    if (EditingFT.DebitAccount == 0) { ModelState.TryAddModelError(nameof(EditingFT.DebitAccount), errorMessage: "A values is required"); }
                    break;
            }
            if (AccountKey.Trim().Split(' ').Length > 4) ModelState.TryAddModelError("AccountKey", errorMessage: "A maximum of 4 words are allowed");
            if (TempData["OrigTD"] != null) //guard against change in Transaction Detail if the IdSource > 1000000
            {
                if (TempData["OrigTD"]?.ToString() != EditingFT.TransactionDetail && EditingFT.IdSource > 1000000)
                {
                    ModelState.TryAddModelError(nameof(EditingFT.TransactionDetail), errorMessage: "Change of transaction detail is not allowed");
                }
            }
            if (!ModelState.IsValid)
            {
                FAList = await _fillService.GetFinancialAccountsList();
                NameContract = await _myDetailsService.GetAccountNameByID(EditingFT.ContractIdentifier);
                return Page();
            }
            // Create TransactionKey
            if (IsCreditTheSource >= 0)
            {
                //record transaction key for the IdSource
                int target = IsCreditTheSource == 1 ? EditingFT.DebitAccount : EditingFT.CreditAccount;
                TransacDictionary td = new()
                {
                    IdKey = GeneratorID.TransDictKeyId(AccountKey),
                    IdSource = EditingFT.IdSource,
                    IdTarget = target,
                    Name = AccountKey
                };
                _dbContext.TransacDictionary.Add(td);
            }
            switch (IsCreditTheSource)
            {
                case -1: //Both Debit and Credit cannot be updated; No AccountKey
                    break;
                case 0: // Credit account is updated and AccountKey is processed
                    var debl = await _dbContext.FinTransactions.Where(p => p.IdSource == EditingFT.IdSource && p.DebitAccount == EditingFT.DebitAccount && p.CreditAccount == 0).ToListAsync();
                    if (debl.Count > 0)
                    {
                        foreach (var d in debl)
                        {
                            if (d.TransactionDetail.Contains(AccountKey))
                            {
                                d.CreditAccount = EditingFT.CreditAccount;
                                _dbContext.FinTransactions.Attach(d).State = EntityState.Modified;
                            }
                        }
                    }
                    break;
                case 1: //Debit account is updated and AccountKey is processed
                    var cebl = await _dbContext.FinTransactions.Where(p => p.IdSource == EditingFT.IdSource && p.CreditAccount == EditingFT.CreditAccount && p.DebitAccount == 0).ToListAsync();
                    if (cebl.Count > 0)
                    {
                        foreach (var d in cebl)
                        {
                            if (d.TransactionDetail.Contains(AccountKey))
                            {
                                d.DebitAccount = EditingFT.DebitAccount;
                                _dbContext.FinTransactions.Update(d);
                            }
                        }
                    }
                    break;
            }
            try
            {
                int i = await _dbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{i} transaction are updated.";
            }
            catch (DbUpdateException dux)
            {
                TempData["ErrorMessage"] = $"Ooops there was an error updating the transation. \n {dux.Message}";
            }
            return RedirectToPage("IndexFinTransactions", new { rfe = ReturnRoute });
        }

    }
}
