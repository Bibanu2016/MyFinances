using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace MyFinances.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public string LogContent { get; set; } = string.Empty;

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);


        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "app.log");

            if (System.IO.File.Exists(logPath))
            {
                LogContent = System.IO.File.ReadAllText(logPath);
            }
            else
            {
                LogContent = "No log file found.";
            }
        }

    }

}
