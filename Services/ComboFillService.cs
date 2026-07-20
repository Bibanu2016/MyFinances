using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.ViewModels;

namespace MyFinances.Services
{
    public class ComboFillService(AppDBContext dBContext, IConfiguration configuration) : IComboFillService
    {
        private readonly AppDBContext _dbcontext = dBContext;
        private readonly IConfiguration _configuration = configuration;

        public async Task<SelectList> GetAccountCategoryList(short contractCateg)
        {
            var qr = await _dbcontext.AccountCategories.Where(m=>m.IdContractCategory == contractCateg).Select(p=> new { p.IdAccountCategory, p.AccountCategoryName}).AsNoTracking().ToListAsync();
            return new SelectList(qr, nameof(AccountCategory.IdAccountCategory), nameof(AccountCategory.AccountCategoryName));
        }

        public async Task<SelectList> GetAllAccountCategoriesList()
        {
            var qr = await _dbcontext.AccountCategories.Select(p => new { p.IdAccountCategory, p.AccountCategoryName }).AsNoTracking().ToListAsync();
            return new SelectList(qr, nameof(AccountCategory.IdAccountCategory), nameof(AccountCategory.AccountCategoryName));
        }

        public async Task<SelectList> GetNTAssetAccountsList()
        {
            var qr = await _dbcontext.AccountCategories.Where(q=>q.IdAccountClass < 20 && q.IdAccountClass > 0).Select(p=> new { p.IdAccountCategory, p.CategoryRoles}).AsNoTracking().ToListAsync();
            IEnumerable<int> temp = [];
            if (qr != null)
            {
                foreach (var item in qr)
                {
                    if (MyBitOperations.GetBit(item.CategoryRoles[0], 0)) temp = temp.Append(item.IdAccountCategory);
                }
            }
            var fal = await _dbcontext.FinAccounts.Where(p=>p.AccountCategory < 2000 && p.AccountCategory >= 1000).AsNoTracking().ToListAsync();
            List<FinAccount> pal = [];
            if (fal != null)
            {

                foreach (var item in fal)
                {
                    if (temp.Contains(item.AccountCategory)) { pal.Add(item); }
                }
            }
            return new SelectList(pal, nameof(FinAccount.IdFinAccount), nameof(FinAccount.Name));
        }

        public async Task<SelectList> GetFinancialAccountsList()
        {
            var fal = await _dbcontext.FinAccounts.Select(p=>new { p.IdFinAccount, p.Name}).AsNoTracking().ToListAsync();
            return new SelectList(fal, nameof(FinAccount.IdFinAccount), nameof(FinAccount.Name));
        }

        public async Task<SelectList> GetOrganisationsList()
        {
            var orgl = await _dbcontext.Organisations.Select(p => new { p.IdOrg, p.OrganisationName }).AsNoTracking().ToListAsync();
            return new SelectList(orgl, nameof(Organisation.IdOrg), nameof(Organisation.OrganisationName));
        }

        public async Task<SelectList> GetOrgTypesList()
        {
            var otl = await _dbcontext.OrganisationTypes.AsNoTracking().ToListAsync();
            return new SelectList(otl, nameof(OrganisationType.Id), nameof(OrganisationType.CategoryName));
        }

        public async Task<SelectList> GetPayableAccountsList()
        {
            var qr = await _dbcontext.AccountCategories.Where(q=>q.IdAccountClass < 30 && q.IdAccountClass > 0).Select(p => new { p.IdAccountCategory, p.CategoryRoles }).AsNoTracking().ToListAsync();
            IEnumerable<int> temp = [];
            if (qr != null)
            {
                foreach (var item in qr)
                {
                    if (MyBitOperations.GetBit(item.CategoryRoles[0], 0) || MyBitOperations.GetBit(item.CategoryRoles[0], 1)) temp = temp.Append<int>(item.IdAccountCategory);
                }
            }
            var fal = await _dbcontext.FinAccounts.Where(p=>p.AccountCategory < 3000 && p.AccountCategory >= 1000).AsNoTracking().ToListAsync();
            List<FinAccount> pal = [];
            if (fal != null)
            {

                foreach (var item in fal)
                {
                    if (temp.Contains(item.AccountCategory)) { pal.Add(item); }
                }
            }
            return new SelectList(pal, nameof(FinAccount.IdFinAccount), nameof(FinAccount.Name));
        }

        public async Task<SelectList> GetUsersList()
        {
            var uli = await _dbcontext.UserContracts.Select(p=> new { p.IdUser, p.FullName}).AsNoTracking().ToListAsync();
            return new SelectList(uli, nameof(UserContract.IdUser), nameof(UserContract.FullName));
        }

        public async Task<SelectList> GetPaymentFrequencyList()
        {
            var pfl = await _dbcontext.PayFrequencys.Select(p => new { p.Id, p.Name }).AsNoTracking().ToListAsync();
            return new SelectList(pfl, nameof(PayFrequency.Id), nameof(PayFrequency.Name));
        }

        public async Task<SelectList> GetContractEndActionList()
        {
            var ceal = await _dbcontext.ContractEndActions.AsNoTracking().ToListAsync();
            return new SelectList(ceal, nameof(ContractEndAction.ID), nameof(ContractEndAction.Name));
        }

        public async Task<SelectList> GetTangibleAssetsList()
        {
            var tal = await _dbcontext.TangibleAssets.AsNoTracking().ToListAsync();
            return new SelectList(tal, nameof(TangibleAsset.IdAsset), nameof(TangibleAsset.Name));
        }

        public async Task<SelectList> GetSuperFinAccountsList()
        {
            int sac = _configuration.GetSection("AccountCategoriesSettings").GetValue<int>("Superannuation");
            var fal = await _dbcontext.FinAccounts.Where(p=>p.AccountCategory == sac).Select(p=> new { p.IdFinAccount, p.Name}).AsNoTracking().ToListAsync();
            return new SelectList(fal, nameof(FinAccount.IdFinAccount), nameof(FinAccount.Name));
        }

        public async Task<SelectList> GetSourcesListForTransacKeys()
        {
            //get list of AccountCategory constrained to AccountClass [10-30) and fields IdAccountCategory and CategoryRoles
            var rawACList = await _dbcontext.AccountCategories.Where(p => p.IdAccountClass >= 10 && p.IdAccountClass < 30).Select(s => new { s.IdAccountCategory, s.CategoryRoles }).AsNoTracking().ToListAsync();
            if (rawACList.Count == 0) return default!;
            //eliminate the values from the list which do not have set the bit indiex 2 to 1 in the CategoryRoles[0]
            List<int> sourcesCtgList = [];
            foreach (var rawAC in rawACList)
            {
                if (MyBitOperations.GetBit(rawAC.CategoryRoles[0],2)) sourcesCtgList.Add(rawAC.IdAccountCategory);
            }
            if(sourcesCtgList.Count == 0){ return default!; }
            //get list of FinAccount with an AccountCategory value betwen [1000, 3000) - Assets and Liabilities
            var faList = await _dbcontext.FinAccounts.Where(p=>p.AccountCategory >= 1000 && p.AccountCategory < 3000).Select(s=> new { s.IdFinAccount, s.AccountCategory, s.Name}).AsNoTracking().ToListAsync();
            if(faList.Count == 0){ return default!; }
                        //filter the FinAccount liste to only those items where the AccountCategory value is in the AccountCategory list
            int tni = faList.Count -1;
            for(int k = tni; k >=0; k--)
            {
                if (!sourcesCtgList.Contains(faList[k].AccountCategory)) faList.RemoveAt(k);
            }
            //return SelectList with IdFinaccount = Key and Name = Value
            return new SelectList(faList, nameof(FinAccount.IdFinAccount), nameof(FinAccount.Name));
        }

        public async Task<SelectList> GetMatchingTransactions(PlanTransaction planTransaction)
        {
            if (planTransaction == null) return default!;
            //get all planned transactions
            var eptl = await _dbcontext.PlanTransactions.
                Where(p => p.DebitAccount == planTransaction.DebitAccount && p.CreditAccount == planTransaction.CreditAccount && p.TransactionDate <= planTransaction.TransactionDate && p.ExecutedTransaction == false).
                OrderBy(x => x.TransactionDate).ToListAsync();
            DateTime lastDate = planTransaction.TransactionDate.AddYears(-1);
            if(eptl.Count > 0)
            {
                lastDate = eptl[0].TransactionDate;
            }
            var ftl = await _dbcontext.FinTransactions.Where(p=>p.DebitAccount == planTransaction.DebitAccount && p.CreditAccount == planTransaction.CreditAccount && p.TransactionDate >= lastDate).OrderBy(x=>x.TransactionDate).
                Select(s => new { ID = s.IdTransaction, NAME = s.TransactionDate.ToShortDateString() + " " + s.DebitAccount.ToString() + "-" + s.CreditAccount.ToString() + " $" + s.TransactionValue.ToString() + " " + s.TransactionDetail}).ToListAsync();
            return new SelectList(ftl, "ID", "NAME");
        }

        public async Task<SelectList> GetNonSourceFinAccounts()
        {
            //get list of AccountCategory constrained to AccountClass [10-30) and fields IdAccountCategory and CategoryRoles
            var rawACList = await _dbcontext.AccountCategories.Where(p => p.IdAccountClass >= 10 && p.IdAccountClass < 40).Select(s => new { s.IdAccountCategory, s.CategoryRoles }).AsNoTracking().ToListAsync();
            if (rawACList.Count == 0) return default!;
            //eliminate the values from the list which do not have set the bit indiex 2 to 1 in the CategoryRoles[0]
            List<int> sourcesCtgList = [];
            foreach (var rawAC in rawACList)
            {
                if (!MyBitOperations.GetBit(rawAC.CategoryRoles[0], 2)) sourcesCtgList.Add(rawAC.IdAccountCategory);
            }
            if (sourcesCtgList.Count == 0) { return default!; }
            //get list of FinAccount with an AccountCategory value betwen [1000, 3000) - Assets and Liabilities
            var faList = await _dbcontext.FinAccounts.Where(p => p.AccountCategory >= 1000 && p.AccountCategory < 4000).Select(s => new { s.IdFinAccount, s.AccountCategory, s.Name }).AsNoTracking().ToListAsync();
            if (faList.Count == 0) { return default!; }
            //filter the FinAccount liste to only those items where the AccountCategory value is in the AccountCategory list
            int tni = faList.Count - 1;
            for (int k = tni; k >= 0; k--)
            {
                if (!sourcesCtgList.Contains(faList[k].AccountCategory)) faList.RemoveAt(k);
            }
            //return SelectList with IdFinaccount = Key and Name = Value
            return new SelectList(faList, nameof(FinAccount.IdFinAccount), nameof(FinAccount.Name));
        }

        public async Task<SelectList> GetStandardContractTypes(short idThreshold)
        {
            var stdctl = await _dbcontext.ContractTypes.Where(p=>p.IdContractType < idThreshold).Select(s=> new {s.IdContractType, s.ContractTypeName }).AsNoTracking().ToListAsync();
            return new SelectList(stdctl, nameof(ContractType.IdContractType), nameof(ContractType.ContractTypeName));
        }

        public async Task<SelectList> GetSpecialContractTypes(short idThreshold)
        {
            var stdctl = await _dbcontext.ContractTypes.Where(p => p.IdContractType >= idThreshold).Select(s => new { s.IdContractType, s.ContractTypeName }).AsNoTracking().ToListAsync();
            return new SelectList(stdctl, nameof(ContractType.IdContractType), nameof(ContractType.ContractTypeName));
        }
    }
}
