using Microsoft.EntityFrameworkCore;
using MyFinances.Models;
using MyFinances.ViewModels;

namespace MyFinances.Data
{
    public class AppDBContext(DbContextOptions<AppDBContext> contextOptions) : DbContext(contextOptions)
    {
        public DbSet<AccountCategory> AccountCategories { get; set; }
        public DbSet<AccountClass> AccountClasses { get; set; }
        public DbSet<AccountType> AccountTypes { get; set; }
        public DbSet<Balance> Balances { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<ContractEndAction> ContractEndActions { get; set; }
        public DbSet<ContractType> ContractTypes { get; set; }
        public DbSet<CreditCard> CreditCards { get; set; }
        public DbSet<Deposit> Deposits { get; set; }
        public DbSet<Employment> Employments { get; set; }
        public DbSet<FinAccount> FinAccounts { get; set; }
        public DbSet<FinContract> FinContracts { get; set; }
        public DbSet<FinTransaction> FinTransactions { get; set; }
        public DbSet<Income> Incomes { get; set; }
        public DbSet<InvestFund> InvestFunds { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<Organisation> Organisations { get; set; }
        public DbSet<OrganisationType> OrganisationTypes { get; set; }
        public DbSet<PayableType> PayableTypes { get; set; }
        public DbSet<PayFrequency> PayFrequencys { get; set; }
        public DbSet<PlanTransaction> PlanTransactions { get; set; }
        public DbSet<RealBalance> RealBalances { get; set; }
        public DbSet<SharePortfolio> SharePortfolios { get; set; }
        public DbSet<ShareTransaction> ShareTransactions { get; set; }
        public DbSet<TangibleAsset> TangibleAssets { get; set; }
        public DbSet<UserContract> UserContracts { get; set; }
        public DbSet<BalanceCard> BalanceCards { get; set; }
        public DbSet<ContractListVM> ContractListVM { get; set; } = default!;
        public DbSet<TransacDictionary> TransacDictionary { get; set; } = default!;
        public DbSet<CashFlowCateg> CashFlowCategs { set; get; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountCategory>().HasKey(x => x.IdAccountCategory);
            modelBuilder.Entity<AccountClass>().HasKey(x => x.IdAccountClass);
            modelBuilder.Entity<AccountType>().HasKey(x => x.IdAccountType);
            modelBuilder.Entity<Balance>().HasKey(x => x.IdBalance);
            modelBuilder.Entity<Bill>().HasKey(x => x.IdBill);
            modelBuilder.Entity<ContractEndAction>().HasKey(x => x.ID);
            modelBuilder.Entity<ContractType>().HasKey(x => x.IdContractType);
            modelBuilder.Entity<CreditCard>().HasKey(x => x.IdCreditCard);
            modelBuilder.Entity<Deposit>().HasKey(x => x.IdDeposit);
            modelBuilder.Entity<Employment>().HasKey(x => x.IdEmployment);
            modelBuilder.Entity<FinAccount>().HasKey(x => x.IdFinAccount);
            modelBuilder.Entity<FinContract>().HasKey(x => x.IdFinContract);
            modelBuilder.Entity<FinTransaction>().HasKey(x => x.IdTransaction);
            modelBuilder.Entity<Income>().HasKey(x => x.IdIncome);
            modelBuilder.Entity<InvestFund>().HasKey(x => x.IdInvestFund);
            modelBuilder.Entity<Loan>().HasKey(x => x.IdLoan);
            modelBuilder.Entity<Organisation>().HasKey(x => x.IdOrg);
            modelBuilder.Entity<OrganisationType>().HasKey(x => x.Id);
            modelBuilder.Entity<PayableType>().HasKey(x => x.IdPT);
            modelBuilder.Entity<PayFrequency>().HasKey(x => x.Id);
            modelBuilder.Entity<PlanTransaction>().HasKey(x => x.IdTransaction);
            modelBuilder.Entity<RealBalance>().HasKey(x => x.IdRealBalance);
            modelBuilder.Entity<SharePortfolio>().HasKey(x => x.IdShare);
            modelBuilder.Entity<TangibleAsset>().HasKey(x => x.IdAsset);
            modelBuilder.Entity<UserContract>().HasKey(x => x.IdUser);
            modelBuilder.Entity<BalanceCard>().HasKey(x => x.IdBalance);
            modelBuilder.Entity<TransacDictionary>().HasKey(x=>x.IdKey);
            modelBuilder.Entity<CashFlowCateg>().HasKey(x => x.IdCFC);
        }
        
    }
}
