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
    public class Create_ACModel(AppDBContext dBContext, IComboFillService comboFill, IMemoryCache mcache) : PageModel
    {
        private readonly AppDBContext _dbctxt = dBContext;
        private readonly IMemoryCache _mc = mcache;
        private readonly IComboFillService _cfill = comboFill;

        [BindProperty] public AccountCategory NewAC { set; get; } = new();

        public SelectList AccClassesList { private set; get; } = default!;
        public SelectList ContTypesList { private set; get; } = default!;

        [BindProperty] public CategRole CRoles { set; get; } = new();
        public async Task OnGet()
        {
            await FillOutCombos();
            ViewData["Title"] = "Create account category";
        }

        public async Task<IActionResult> OnPost()
        {

            // custom validations
            if ((CRoles.PaymentAsset || CRoles.PaymentLiability) && (CRoles.InvestmentFund || CRoles.UserAccount || CRoles.Tax))
            {
                ModelState.TryAddModelError(nameof(NewAC.CategoryRoles), "Invalid allocation for category role");
            }
            if (!ModelState.IsValid)
            {
                await FillOutCombos();
                return Page();
            }
            //update category roles
            NewAC.CategoryRoles[0] = MyBitOperations.SetBit(NewAC.CategoryRoles[0], 0, CRoles.PaymentAsset);
            NewAC.CategoryRoles[0] = MyBitOperations.SetBit(NewAC.CategoryRoles[0], 1, CRoles.PaymentLiability);
            NewAC.CategoryRoles[0] = MyBitOperations.SetBit(NewAC.CategoryRoles[0], 2, CRoles.ImportTransaction);
            NewAC.CategoryRoles[0] = MyBitOperations.SetBit(NewAC.CategoryRoles[0], 3, CRoles.InvestmentFund);
            NewAC.CategoryRoles[0] = MyBitOperations.SetBit(NewAC.CategoryRoles[0], 4, CRoles.Tax);
            NewAC.CategoryRoles[0] = MyBitOperations.SetBit(NewAC.CategoryRoles[0], 5, CRoles.UserAccount);
            NewAC.CategoryRoles[0] = MyBitOperations.SetBit(NewAC.CategoryRoles[0], 6, CRoles.Spare);
            NewAC.CategoryRoles[0] = MyBitOperations.SetBit(NewAC.CategoryRoles[0], 7, CRoles.Locked);
            //update acc category
            NewAC.IdAccountCategory = await GetACID(NewAC.IdAccountClass);
            _dbctxt.AccountCategories.Add(NewAC);
            try
            {
                await _dbctxt.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Account category {NewAC.AccountCategoryName} \n was successfully updated";
            }
            catch (DbException dex)
            {
                TempData["ErrorMessage"] = $"Ooops there was an error updating {NewAC.AccountCategoryName} \n {dex.Message}";
            }
            return RedirectToPage("Index_AC");
        }

        private async Task FillOutCombos()
        {
            if (_mc.TryGetValue("ACL", out SelectList? acl))
            {
                if (acl is not null)
                {
                    AccClassesList = acl;
                }
            }
            else
            {
                AccClassesList = await _cfill.GetAccountClassesList();
                _mc.Set("ACL", AccClassesList);
            }
            if (_mc.TryGetValue("CCL", out SelectList? ccl))
            {
                if (ccl is not null) { ContTypesList = ccl; }
            }
            else
            {
                ContTypesList = await _cfill.GetStandardContractTypes(100);
                _mc.Set("CCL", ContTypesList);
            }
            return;
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

        private async Task<int> GetACID(short classID)
        {
            var acl = await _dbctxt.AccountCategories.Where(p => p.IdAccountClass == classID).Select(s => s.IdAccountCategory).Order().ToListAsync();
            if (acl.Count > 0)
            {
                return acl.Max() + 1;
            }
            else
            {
                return classID * 100 + 1;
            }
        }


    }
}
