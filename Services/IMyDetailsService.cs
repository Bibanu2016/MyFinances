namespace MyFinances.Services
{
    public interface IMyDetailsService
    {
        Task<string> GetAccountNameByID(int idAccount);
        Task<string> GetAccountCategNameByID(int idCateg);
        Task<string> GetOrganisationNameByID(int idOrganisation);
        Task<string> GetAccountClassByID(int idAccClass);

        Task<string> GetAccountTypeByID(int idAccType);

        Task<string> GetContractNameByID(Guid idContract);
        Task<string> GetContractTypeByID(short idContractType);

        Task<string> GetOrganisationTypeByID(int idOrgType);

        Task<string> GetFrequencyNameByID(int idFrequency);

        Task<string> GetAssetNameByID(Guid idAsset);

        Task<string> GetUserNameByID(int idUser);

        Task<string> GetEndActionNameByID(short idEndAction);

        Task<string> GetLoanSecurityNameById(Guid loanSec);

    }
}
