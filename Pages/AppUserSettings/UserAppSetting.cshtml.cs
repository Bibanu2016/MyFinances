using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyFinances.Data;
using MyFinances.Models;
using System.ComponentModel;

namespace MyFinances.Pages.AppUserSettings
{
    public class UserAppSettingModel(AppDBContext dBContext, IConfiguration configuration) : PageModel
    {
        private readonly AppDBContext _context = dBContext;
        private readonly IConfiguration _config = configuration;

        [BindProperty] public UserContract UserCont { set; get; } = new();
        public void OnGet()
        {

        }

        public class AppUserSet()
        {
            public int StartFY { set; get; } = 7;
            public int NumberYearPlan { set; get; } = 5;
            public int RatioCashRepay { set; get; } = 10;
            public int CCPayOffLimit { set; get; } = 100;
        }
    }
}
