using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyFinances.Services;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Pages.FinTransactions
{
    public class UploadForDictionaryKeyModel(IMyCsvService csvService) : PageModel
    {
        private readonly IMyCsvService _csvService = csvService;
        [BindProperty, Required(ErrorMessage = "A file selection is required!")]
        public IFormFile? Upload { get; set; } = default!;

        public string? JsonOutput { get; set; }

        public void OnGet()
        {
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
                    ModelState.AddModelError("Upload", "Invalid file type. Only csv or Excel files are permited");
                }
                if (Upload.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Upload", "File too large.");
                }
                using var ts = Upload.OpenReadStream();
                if (!_csvService.CsvFileIsValid(ts))
                {
                    ModelState.AddModelError("Upload", "A valid file strucure is required (date, decimal, text fields with maximum 5 columns)");
                }
            }
            if (!ModelState.IsValid)
            {
                return Page();
            }
            /*
             * 
             * var uploadsFolder = Path.Combine("wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, Upload.FileName);
             * using (var stream = System.IO.File.Create(filePath))
            {
                await Upload.CopyToAsync(stream);
            }
            using var ms = new MemoryStream();
            await Upload.CopyToAsync(ms);
            byte[] data = ms.ToArray();
             */
            using var stream = Upload.OpenReadStream();
            using var reader = new StreamReader(stream);
            string csvContent = await reader.ReadToEndAsync();
            HttpContext.Session.SetString("CsvImport", csvContent);
            return RedirectToPage("ImportColumnSelection");
        }


    }
}
