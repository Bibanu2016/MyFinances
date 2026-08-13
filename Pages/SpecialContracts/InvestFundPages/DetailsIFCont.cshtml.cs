using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using MyFinances.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MyFinances.Pages.SpecialContracts.InvestFundPages
{
    public class DetailsIFContModel(AppDBContext context, IMyDetailsService detailsService) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IMyDetailsService _details = detailsService;

        public List<ItemDetail> ICDetails { set; get; } = new List<ItemDetail>(26);
        public decimal LastRealBalance { set; get; } = 0;

        public DateTime LRBalanceDate { set; get; } = DateTime.Today;

        public bool IsExecuted { set; get; } = false;

        public string ItemID { private set; get; } = Guid.Empty.ToString();



        public async Task<IActionResult> OnGet(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            if(!Guid.TryParse(id, out _)) { return NotFound(); }
            Guid gid = Guid.Parse(id);
            ItemID = id;
            var mc = await _context.FinContracts.FindAsync(gid);
            var imf = await _context.InvestFunds.FindAsync(gid);
            if(mc is null || imf is null)
            {
                return NotFound();
            }
            ViewData["Title"] = $"Details for contract: {mc.ContractName}";
            await FillUpList(mc, imf);
            FinContractHelper fch = new(_context, default!);
            bool contIsExecuted = await fch.GetContractExecutionState(gid);
            if(contIsExecuted)
            {
                LastRealBalance = await fch.GetContractRealBalance(mc.IdFinAccount, DateTime.Today);
                LRBalanceDate = await fch.GetDateLastTransac(true, mc.IdFinAccount, DateTime.Today);
                IsExecuted = contIsExecuted;
            }
            return Page();
        }

        private async Task FillUpList(FinContract mainCont, InvestFund investFund)
        {
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.ContractType)), ItemValue = await _details.GetContractTypeByID(mainCont.ContractType) });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.ContractName)), ItemValue = mainCont.ContractName });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.ContractDetails)), ItemValue = mainCont.ContractDetails });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdOrganisation)), ItemValue = await _details.GetOrganisationNameByID(mainCont.IdOrganisation) });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdContractor)), ItemValue = await _details.GetUserNameByID(mainCont.IdContractor) });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.StartDate)), ItemValue = mainCont.StartDate.ToString("d") });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.EndDate)), ItemValue = "until user cancellation" });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.InvoiceDate)), ItemValue = mainCont.InvoiceDate.ToShortDateString()});
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.InvoiceNetDays)), ItemValue = "0" });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdFrequency)), ItemValue = await _details.GetFrequencyNameByID(mainCont.IdFrequency) });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdFinAccount)), ItemValue = mainCont.IdFinAccount.ToString() + " : " + await _details.GetAccountNameByID(mainCont.IdFinAccount) });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdPayAccount)), ItemValue = "Not required" }); //Pay Account
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IdBankAccount)), ItemValue = "Not required" }); //Bank Account
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.IsActiveContract)), ItemValue = mainCont.IsActiveContract.ToString() });
            if(mainCont.ClosedDate <= DateTime.Today)
            {
                ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.ClosedDate)), ItemValue = mainCont.ClosedDate.ToShortDateString() });
            }
            else
            {
                ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<FinContract>(nameof(FinContract.ClosedDate)), ItemValue = "Active contract" });
            }
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.AdviserFee)), ItemValue = investFund.AdviserFee.ToString() });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.ManagementFee)), ItemValue = investFund.ManagementFee.ToString() });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.AdminFee)), ItemValue = investFund.AdminFee.ToString() });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.AccountingFee)), ItemValue = investFund.AccountingFee.ToString() });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdCapitalGainAcc)), ItemValue = investFund.IdCapitalGainAcc.ToString() + " : " + await _details.GetAccountNameByID(investFund.IdCapitalGainAcc) });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdAdviseAcc)), ItemValue = investFund.IdAdviseAcc.ToString() + " : " + await _details.GetAccountNameByID(investFund.IdAdviseAcc) });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdManageAcc)), ItemValue = investFund.IdManageAcc.ToString() + " : " + await _details.GetAccountNameByID(investFund.IdManageAcc) });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdAdminAcc)), ItemValue = investFund.IdAdminAcc.ToString() + " : " + await _details.GetAccountNameByID(investFund.IdAdminAcc) });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdAccountingAcc)), ItemValue = investFund.IdAccountingAcc.ToString() + " : " + await _details.GetAccountNameByID(investFund.IdAccountingAcc) });
            ICDetails.Add(new ItemDetail() { ItemName = GetDisplayName<InvestFund>(nameof(InvestFund.IdTaxAccount)), ItemValue = investFund.IdTaxAccount.ToString() + " : " + await _details.GetAccountNameByID(investFund.IdTaxAccount) });
            return;
        }

        public static string GetDisplayName<T>(string propertyName)
        {
            var prop = typeof(T).GetProperty(propertyName);

            if (prop == null)
                return propertyName; // fallback

            var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();

            return displayAttr?.Name ?? propertyName;
        }


    }
}
