using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.Data.Common;

namespace MyFinances.Pages.AccountCategoryPages
{
    public class Edit_ACModel(AppDBContext dBContext, IMyDetailsService detailsService, IComboFillService comboFill, IMemoryCache mcache) : PageModel
    {
        private readonly AppDBContext _dbctxt = dBContext;
        private readonly IMyDetailsService _details = detailsService;
        private readonly IMemoryCache _mc = mcache;
        private readonly IComboFillService _cfill = comboFill;

        [BindProperty]
        public AccountCategory AccCategEdit { set; get; } = default!;
        public SelectList ContTypeList { private set; get; } = default!;
        public string AccClassName { set; get; } = string.Empty;
        public string StringCategRole { set; get; } = string.Empty;

        [BindProperty(SupportsGet =true)]
        public CategRole CRole { set; get; } = new();
        
        public async Task<IActionResult> OnGet(int id)
        {
            AccCategEdit = await _dbctxt.AccountCategories.Where(p=>p.IdAccountCategory == id).FirstOrDefaultAsync() ?? default!;
            if(AccCategEdit is null) { return NotFound(); }
            // get Account Class Name value
            AccClassName = await _mc.GetOrCreateAsync("ACN", () => _details.GetAccountClassByID(AccCategEdit.IdAccountClass));
            // get list for ContractType
            ContTypeList = await _mc.GetOrCreateAsync("CTL", () => _cfill.GetStandardContractTypes(100));
            // deconstruct values from CategoryRole
            CRole.PaymentAsset = MyBitOperations.GetBit(AccCategEdit.CategoryRoles[0], 0);
            CRole.PaymentLiability = MyBitOperations.GetBit(AccCategEdit.CategoryRoles[0], 1);
            CRole.ImportTransaction = MyBitOperations.GetBit(AccCategEdit.CategoryRoles[0], 2);
            CRole.InvestmentFund = MyBitOperations.GetBit(AccCategEdit.CategoryRoles[0], 3);
            CRole.Tax = MyBitOperations.GetBit(AccCategEdit.CategoryRoles[0], 4);
            CRole.UserAccount = MyBitOperations.GetBit(AccCategEdit.CategoryRoles[0], 5);
            CRole.Spare = MyBitOperations.GetBit(AccCategEdit.CategoryRoles[0], 6);
            CRole.Locked = MyBitOperations.GetBit(AccCategEdit.CategoryRoles[0], 7);
            StringCategRole = AccCategEdit.CategoryRoles[0].ToString("b").PadLeft(8, '0');
            ViewData["Title"] = $"Edit account category id: {AccCategEdit.IdAccountCategory}";
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {

            // custom validations
            if ((CRole.PaymentAsset || CRole.PaymentLiability) && (CRole.InvestmentFund || CRole.UserAccount || CRole.Tax))
            {
                ModelState.TryAddModelError(nameof(AccCategEdit.CategoryRoles), "Invalid allocation for category role");
            }
            if (!ModelState.IsValid)
            {
                AccClassName = await _mc.GetOrCreateAsync("ACN", () => _details.GetAccountClassByID(AccCategEdit.IdAccountClass));
                ContTypeList = await _mc.GetOrCreateAsync("CTL", () => _cfill.GetStandardContractTypes(100));
                return Page();
            }
            //update category roles
            AccCategEdit.CategoryRoles[0] = MyBitOperations.SetBit(AccCategEdit.CategoryRoles[0], 0, CRole.PaymentAsset);
            AccCategEdit.CategoryRoles[0] = MyBitOperations.SetBit(AccCategEdit.CategoryRoles[0], 1, CRole.PaymentLiability);
            AccCategEdit.CategoryRoles[0] = MyBitOperations.SetBit(AccCategEdit.CategoryRoles[0], 2, CRole.ImportTransaction);
            AccCategEdit.CategoryRoles[0] = MyBitOperations.SetBit(AccCategEdit.CategoryRoles[0], 3, CRole.InvestmentFund);
            AccCategEdit.CategoryRoles[0] = MyBitOperations.SetBit(AccCategEdit.CategoryRoles[0], 4, CRole.Tax);
            AccCategEdit.CategoryRoles[0] = MyBitOperations.SetBit(AccCategEdit.CategoryRoles[0], 5, CRole.UserAccount);
            AccCategEdit.CategoryRoles[0] = MyBitOperations.SetBit(AccCategEdit.CategoryRoles[0], 6, CRole.Spare);
            AccCategEdit.CategoryRoles[0] = MyBitOperations.SetBit(AccCategEdit.CategoryRoles[0], 7, CRole.Locked);
            //update acc category
            _dbctxt.AccountCategories.Update(AccCategEdit);
            try
            {
                await _dbctxt.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Account category {AccCategEdit.AccountCategoryName} \n was successfully updated";
            }
            catch(DbException dex)
            {
                TempData["ErrorMessage"] = $"Ooops there was an error updating {AccCategEdit.AccountCategoryName} \n {dex.Message}";
            }
            return RedirectToPage("Index_AC");
        }

        public class CategRole()
        {
            public bool PaymentAsset { set; get; } = false;
            public bool PaymentLiability { set; get; } = false;
            public bool ImportTransaction { set; get; } = false;
            public bool InvestmentFund { set; get; } = false;
            public bool Tax { set; get; } = false;
            public bool UserAccount { set; get; } = false;
            public bool Spare { set; get; } = false;
            public bool Locked { set; get; } = false;
        }
    }
}
