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
        Task<SelectList> GetOrganisationsList();
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

    }
}
