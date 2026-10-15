using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyFinances.Services;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;

namespace MyFinances.Pages.FinTransactions
{
    public class ImportColumnSelectionModel(IMyCsvService myCsvService) : PageModel
    {
        private readonly IMyCsvService _myCsvService = myCsvService;
        public string? UploadString { get; set; }
        public DataTable ImportTable { get; set; } = new DataTable();

        public List<int> ColsIndex { get; set; } = [];

        [BindProperty, Range(0, 5, ErrorMessage = "A value between 0 and 5 is required")]
        public int IndexDetail { get; set; }
        public void OnGet()
        {
            if (HttpContext.Session.TryGetValue("CsvImport", out byte[]? data))
            {
                if (data.Length > 0)
                {
                    string result = Encoding.UTF8.GetString(data);
                    ImportTable = _myCsvService.CsvToDataTable(result);
                    for (int k = 0; k < ImportTable.Columns.Count; k++)
                    {
                        string colval = ImportTable.Rows[0].Field<string>(k) ?? string.Empty;
                        if (DateTime.TryParse(colval, out _) || Double.TryParse(colval, out _)) { continue; }
                        else { ColsIndex.Add(k); }
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "No data has been received";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "No data has been received";
            }
            return;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            return RedirectToPage("ImportedKeySelection", new { idxcol = IndexDetail });
        }

        public async Task<IActionResult> OnPostGoback()
        {
            // clear the Session data
            HttpContext.Session.Clear();
            // return to the upload page
            return RedirectToPage("UploadForDictionaryKey");
        }

    }
}
