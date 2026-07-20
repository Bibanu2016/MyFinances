using CsvHelper;
using CsvHelper.Configuration;
using Elfie.Serialization;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MyFinances.Pages.FinTransactions
{
    public class ImportTransactionsModel(AppDBContext dbContext, IMemoryCache cache, IMyCsvService csvService, IDataTableJsonService tableJsonService) : PageModel
    {
        private readonly AppDBContext _context = dbContext;
        private readonly IMemoryCache _cache = cache;
        private readonly IMyCsvService _csvService = csvService;
        private readonly IDataTableJsonService _tableJsonService = tableJsonService;
        [BindProperty]
        public IFormFile? Upload { get; set; } = default!;
        [BindProperty, Required, Range(1000000, 3000000, ErrorMessage =$"A selection is required")]
        public int ImportSourceID { get; set; } = 0;

        public IList<ViewModels.ComboIntStrLVM> AccountsList { get; set; } = [];
        public async Task<IActionResult> OnGet()
        {

            AccountsList = await _cache.GetOrCreateAsync("AccList", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                entry.SlidingExpiration = TimeSpan.FromMinutes(2);

                // Simulate DB call
                return await TransactionSourcesAccList();
            }) ?? [];
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Upload == null || Upload.Length == 0)
            {
                ModelState.AddModelError("Upload", "Please select a file.");
                
            }
            else
            {
                var allowed = new[] { ".csv" };
                var ext = Path.GetExtension(Upload.FileName).ToLowerInvariant();

                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("Upload", "Invalid file type. Only csv files are permited");
                }

                if (Upload.Length > 1 * 1024 * 1024)
                {
                    ModelState.AddModelError("Upload", "File too large. Maximum file size is 1 Gb.");
                }
                using Stream stream1 = Upload.OpenReadStream();
                bool validFile = _csvService.CsvFileIsValid(stream1);
                if(!validFile)
                {
                    ModelState.TryAddModelError("Upload", "The file content appear invalid. \n Numbers of columns between 3 to 5, a Date and Number field are required.");
                }
            }
            if (!ModelState.IsValid)
            {
                AccountsList = _cache.Get<List<ViewModels.ComboIntStrLVM>>("AccList") ?? [];
                return Page();
            }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            using Stream stream2 = Upload.OpenReadStream();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            using var reader = new StreamReader(stream2);
            var csvContent = await reader.ReadToEndAsync();
            HttpContext.Session.SetString("TIImport", csvContent);
            _cache.Remove("AccList");
            return RedirectToPage("ColumnsSelectionTransImp", new { accid = ImportSourceID });
        }

        private async Task<List<ViewModels.ComboIntStrLVM>> TransactionSourcesAccList()
        {
            List<ViewModels.ComboIntStrLVM> returnlist = [];
            var result = await _context.FinAccounts
                        .Join(_context.AccountCategories, c => c.AccountCategory, o => o.IdAccountCategory,
                            (c, o) => new
                            {
                                c.IdFinAccount,
                                c.Name,
                                o.CategoryRoles
                            }).OrderBy(x => x.IdFinAccount).ToListAsync();
            if (result.Count > 0)
            {
                foreach (var item in result)
                {
                    if (MyBitOperations.GetBit(item.CategoryRoles[0], 2))
                    {
                        returnlist.Add(new ViewModels.ComboIntStrLVM { ItemID = item.IdFinAccount, Name = item.Name });
                    }
                }
            }
            return returnlist;
        }

    }
}
