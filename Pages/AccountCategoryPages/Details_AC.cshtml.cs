using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Services;
using MyFinances.ViewModels;
using System.Text;

namespace MyFinances.Pages.AccountCategoryPages
{
    public class Details_ACModel(AppDBContext dBContext, IMyDetailsService detailsService) : PageModel
    {
        private readonly AppDBContext _dbctxt = dBContext;
        private readonly IMyDetailsService _details = detailsService;

        public List<ItemDetail> ACDetails { set; get; } = [];

        [BindProperty] public int IDCateg { set; get; } = 0;

        public async Task<IActionResult> OnGet(int id)
        {
            var aca = await _dbctxt.AccountCategories.FindAsync(id);
            if (aca is null) { return NotFound(); }
            IDCateg = id;
            //fill out items details
            var temp = await _details.GetAccountClassByID(aca.IdAccountClass);
            ACDetails.Add(new ItemDetail() { ItemName = "Account Class", ItemValue = temp });
            ACDetails.Add(new ItemDetail() { ItemName = "Name", ItemValue = aca.AccountCategoryName });
            temp = await _details.GetContractTypeByID(aca.IdContractCategory);
            ACDetails.Add(new ItemDetail() { ItemName = "Contract category", ItemValue = temp });
            StringBuilder sb = new StringBuilder();
            string[] rn = ["Paying asset", "Paying liability", "Import transactions", "Invest fund", "Tax", "User account", "Spare", "Locked for editing"];
            for (int i = 0; i < 8; i++)
            {
                if (MyBitOperations.GetBit(aca.CategoryRoles[0], i))
                {
                    sb.Append(rn[i]);
                    sb.Append(", ");
                }
            }
            ACDetails.Add(new ItemDetail() { ItemName = "Category roles", ItemValue = sb.ToString() });
            var ta = await _dbctxt.FinAccounts.Where(p => p.AccountCategory == aca.IdAccountCategory).CountAsync();
            ACDetails.Add(new ItemDetail() { ItemName = "Total number accounts held", ItemValue = ta.ToString() });
            ViewData["Title"] = $"Details account category id: {aca.IdAccountCategory}";
            return Page();
        }
    }
}
