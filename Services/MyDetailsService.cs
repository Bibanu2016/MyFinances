using Microsoft.EntityFrameworkCore;
using MyFinances.Data;

namespace MyFinances.Services
{
    public class MyDetailsService(AppDBContext appDBContext) : IMyDetailsService
    {
        private readonly AppDBContext _appDBContext = appDBContext;
        public async Task<string> GetAccountCategNameByID(int idCateg)
        {
            var result = await _appDBContext.AccountCategories.Where(p=>p.IdAccountCategory == idCateg).Select(s=>s.AccountCategoryName).AsNoTracking().FirstOrDefaultAsync();
            result ??= string.Empty;
            return result;
        }

        public async Task<string> GetAccountClassByID(int idAccClass)
        {
            return await _appDBContext.AccountClasses.Where(p=>p.IdAccountClass == idAccClass).Select(s=>s.AccountClassName).AsNoTracking().FirstOrDefaultAsync()??string.Empty;
        }

        public async Task<string> GetAccountNameByID(int idAccount)
        {
            return await _appDBContext.FinAccounts.Where(p => p.IdFinAccount == idAccount).Select(s => s.Name).AsNoTracking().FirstOrDefaultAsync() ?? string.Empty;
        }

        public async Task<string> GetAccountTypeByID(int idAccType)
        {
            return await _appDBContext.AccountTypes.Where(p=>p.IdAccountType == idAccType).Select(s=>s.AccountTypeName).AsNoTracking().FirstOrDefaultAsync()??string.Empty;
        }

        public async Task<string> GetAssetNameByID(Guid idAsset)
        {
            return await _appDBContext.TangibleAssets.Where(p => p.IdAsset == idAsset).Select(s => s.Name).AsNoTracking().FirstOrDefaultAsync() ?? string.Empty;
        }

        public async Task<string> GetContractNameByID(Guid idContract)
        {
            return await _appDBContext.FinContracts.Where(p => p.IdFinContract == idContract).Select(s => s.ContractName).AsNoTracking().FirstOrDefaultAsync() ?? string.Empty;
        }

        public async Task<string> GetContractTypeByID(short idContractType)
        {
            return await _appDBContext.ContractTypes.Where(p=>p.IdContractType == idContractType).Select(s=>s.ContractTypeName).AsNoTracking().FirstOrDefaultAsync() ?? string.Empty;
        }

        public async Task<string> GetEndActionNameByID(short idEndAction)
        {
            return await _appDBContext.ContractEndActions.Where(p=>p.ID == idEndAction).Select(s=>s.Name).FirstOrDefaultAsync() ?? string.Empty;
        }

        public async Task<string> GetFrequencyNameByID(int idFrequency)
        {
            return await _appDBContext.PayFrequencys.Where(p => p.Id == idFrequency).Select(s => s.Name).AsNoTracking().FirstOrDefaultAsync() ?? string.Empty;
        }

        public async Task<string> GetOrganisationNameByID(int idOrganisation)
        {
            return await _appDBContext.Organisations.Where(p => p.IdOrg == idOrganisation).Select(s => s.OrganisationName).AsNoTracking().FirstOrDefaultAsync() ?? string.Empty;
        }

        public async Task<string> GetOrganisationTypeByID(int idOrgType)
        {
            return await _appDBContext.OrganisationTypes.Where(p => p.Id == idOrgType).Select(s => s.CategoryName).AsNoTracking().FirstOrDefaultAsync()??string.Empty;
        }

        public async Task<string> GetUserNameByID(int idUser)
        {
            return await _appDBContext.UserContracts.Where(p => p.IdUser == idUser).Select(s => s.FullName).AsNoTracking().FirstOrDefaultAsync() ?? string.Empty;
        }

    }
}
