using Microsoft.AspNetCore.Mvc.Rendering;
using MyFinances.Models;
using MyFinances.ViewModels;

namespace MyFinances.Services
{
    public interface IComboFillService
    {

        Task<SelectList> GetFinancialAccountsList();
        Task<SelectList> GetPayableAccountsList();
        Task<SelectList> GetNTAssetAccountsList();
        Task<SelectList> GetAccountCategoryList(short contractCateg);
        Task<SelectList> GetAllAccountCategoriesList();
        Task<SelectList> GetOrganisationsList(short categoryOrg = 0);
        Task<SelectList> GetOrgTypesList();
        Task<SelectList> GetUsersList();
        Task<SelectList> GetPaymentFrequencyList();
        Task<SelectList> GetContractEndActionList();
        Task<SelectList> GetTangibleAssetsList();
        Task<SelectList> GetSuperFinAccountsList();
        Task<SelectList> GetSourcesListForTransacKeys();
        Task<SelectList> GetMatchingTransactions(PlanTransaction planTransaction);
        Task<SelectList> GetNonSourceFinAccounts();
        Task<SelectList> GetStandardContractTypes(short idThreshold);
        Task<SelectList> GetSpecialContractTypes(short idThreshold);
        Task<SelectList> GetFinAccountListByCateg(List<short> categs = default!);
        Task<SelectList> GetFinAccountListByRole(string[] acRoles);
        Task<SelectList> GetAccountTypesList();
        Task<SelectList> GetAccountClassesList();
        Task<SelectList> GetCashFlowCategoriesList();
        Task<SelectList> GetUserTaxAccountsList(int usedID);
    }
}
