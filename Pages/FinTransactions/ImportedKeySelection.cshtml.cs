using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyFinances.Controllers;
using MyFinances.Services;
using System.Data;
using System.Text;
using System.Text.Json;

namespace MyFinances.Pages.FinTransactions
{
    public class ImportedKeySelectionModel(IMyCsvService csvService) : PageModel
    {
        private readonly IMyCsvService _csvService = csvService;

        [BindProperty(SupportsGet = true)]
        public int IdxCol { get; set; }

        [BindProperty]
        public List<string> ImportList { get; set; } = [];

        [BindProperty]
        public List<string> KeyList { get; set; } = [];
       
        public void OnGet(int? idxcol)
        {
            if(idxcol.HasValue)
            {
                IdxCol = idxcol.Value;
                if (HttpContext.Session.TryGetValue("CsvImport", out byte[]? data))
                {
                    if (data.Length > 0)
                    {
                        // clear the session data if has been read and ok
                        HttpContext.Session.Clear();
                        string result = Encoding.UTF8.GetString(data);
                        DataTable dti = _csvService.CsvToDataTable(result);
                        // build the list with the selected column
                        IList<string> detlist = [];
                        foreach (DataRow row in dti.Rows)
                        {
                            detlist.Add(row[IdxCol].ToString() ?? string.Empty);
                        }
                        // order the elements ascending
                        IEnumerable<string> ol = detlist.Order();
                        var unique = ol.Distinct().ToList();
                        ImportList.AddRange(unique);
                        foreach(string _ in ImportList)
                        {
                            KeyList.Add(string.Empty);
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "No data has been received";
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            List<string> Keys = [];
            foreach (var key in KeyList)
            {
                if(key != null) 
                {
                    if(key.Length > 2 && key.Length < 45)
                    {
                        Keys.Add(key.Trim());
                    }
                }
            }
            if(Keys.Count > 0)
            {
                var uniqueList = Keys.Distinct().ToList();
                JsonSerializerOptions jsonser = new()
                {
                    WriteIndented = false
                };
                string JsonOutput = JsonSerializer.Serialize(uniqueList, jsonser);
                HttpContext.Session.SetString("KeyList", JsonOutput);
            }
            return RedirectToPage("KeyAllocation");
        }

    }
}
