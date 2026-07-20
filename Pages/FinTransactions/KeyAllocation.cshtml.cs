using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.ViewModels;
using NuGet.Packaging;
using System.Data;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace MyFinances.Pages.FinTransactions
{
    public class KeyAllocationModel(AppDBContext dbContext) : PageModel
    {
        private readonly AppDBContext _appDBContext = dbContext;
        [BindProperty]
        public List<TransacDictionary> KeyDictList { get; set; } = [];
        [BindProperty]
        public int IdSourceKeys { get; set; } = 0;

        public SelectList FinAccountLI { get; set; } = default!;
        public SelectList SourceLI { get; set; } = default!;

        public async Task OnGet()
        {
            
            await FillOutCombos();
            FillKeyList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if(!ModelState.IsValid)
            {
                FillKeyList();
                return Page();
            }
            HttpContext.Session.Clear();
            foreach (var key in KeyDictList)
            {
                key.IdSource = IdSourceKeys;
            }
            _appDBContext.TransacDictionary.AddRange(KeyDictList);
            // procedure to remove the duplicated keys
            DataRow[] ConcurencyRows = [];
            try
            {
                int x = await _appDBContext.SaveChangesAsync();
                TempData["SuccessMessage"] = $"A total of {x} keys have been recorded!";
            }
            catch(DBConcurrencyException ex)
            {
                ex.CopyToRows(ConcurencyRows);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ex.Row.ClearErrors();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            return RedirectToPage("TransDictIndex");
        }

        private async Task FillOutCombos()
        {
            var acl = await _appDBContext.AccountCategories.Select(s => new { s.IdAccountCategory, s.CategoryRoles }).ToListAsync();
            List<int> CtgSourceList = [];
            foreach (var item in acl)
            {
                if (MyBitOperations.GetBit(item.CategoryRoles[0], 2))
                {
                    CtgSourceList.Add(item.IdAccountCategory);
                }
            }
            var fal = await _appDBContext.FinAccounts.Select(s => new { s.IdFinAccount, s.AccountCategory, s.Name }).OrderBy(x => x.IdFinAccount).ToListAsync();
            if (CtgSourceList.Count > 0 && fal != null)
            {
                FinAccountLI = new(fal, nameof(FinAccount.IdFinAccount), nameof(FinAccount.Name));
                List<ComboIntStrLVM> tempList = [];
                foreach (var item in fal)
                {
                    if (CtgSourceList.Contains(item.AccountCategory)) { tempList.Add(new ViewModels.ComboIntStrLVM { ItemID = item.IdFinAccount, Name = item.Name }); }
                }
                SourceLI = new(tempList, nameof(ComboIntStrLVM.ItemID), nameof(ComboIntStrLVM.Name));
            }
            return;
        }

        private void FillKeyList()
        {
            if (HttpContext.Session.TryGetValue("KeyList", out byte[]? data))
            {
                if (data.Length > 0)
                {
                    string result = Encoding.UTF8.GetString(data);
                    List<string>? items = JsonSerializer.Deserialize<List<string>>(result);
                    if (items != null)
                    {
                        foreach (var item in items)
                        {

                            KeyDictList.Add(new TransacDictionary { IdKey = GeneratorID.TransDictKeyId(item), Name = item });
                        }
                    }
                }
            }
            return ;
        }
    }

}
