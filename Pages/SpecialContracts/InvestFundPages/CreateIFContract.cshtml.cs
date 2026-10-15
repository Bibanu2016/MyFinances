using FinanceCalculator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace MyFinances.Pages.SpecialContracts.InvestFundPages
{
    public class CreateIFContract(AppDBContext context, IConfiguration configuration, IMemoryCache memoryCache, IComboFillService comboFill) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly IComboFillService _comboFill = comboFill;
        //
        // Contract data
        [BindProperty]
        public FinContract MainContract { get; set; } = default!;
        [BindProperty]
        public InvestFund MyInvestFund { get; set; } = default!;
        // contract type selection variable
        [BindProperty, DataType(DataType.Currency), Range(0, 1000000000, ErrorMessage = "Value {0} is required in the range {1} to {2}"), Display(Name = "Current balance")]
        public decimal PurchaseBalance { get; set; } = 0;
        [BindProperty, DataType(DataType.DateTime), Display(Name = "Date of current balance")]
        public DateTime BalanceDate { get; set; } = DateTime.Now;

        [BindProperty] public int CategoryContract { get; set; } = 0;


        // Combobox source data
        public SelectList Frequency { get; set; } = default!;
        public SelectList Organisation { get; set; } = default!;
        public SelectList Users { get; set; } = default!;



        public async Task OnGet()
        {
            short cct = _configuration.GetSection("ContractTypes").GetValue<short>("InvestmentFund");
            if (cct == 0) { CategoryContract = 21; } else { CategoryContract = cct; }
            ViewData["Title"] = "Create Investment Management Fund Contract";
            await FillUpLists();
            MainContract = new() { IdFinContract = Guid.NewGuid() };
            MyInvestFund = new() { IdInvestFund = MainContract.IdFinContract };
        }

        // --- Procedures to create the lists ---
        /// <summary>
        /// Populates the lists with the data from the database
        /// </summary>
        /// <returns></returns>
        private async Task FillUpLists()
        {
            Frequency = await _comboFill.GetPaymentFrequencyList();
            short ifoc = _configuration.GetSection("OrgTypes").GetValue<short>("InvestmentFund");
            Organisation = await _comboFill.GetOrganisationsList(ifoc);
            Users = await _comboFill.GetUsersList();
            return;
        }

        //
        // POST Method
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // --- model validation start ---
            // check if the balance date is before start of the financial year
            DateTime sFY = new GeneralHelper(_configuration).StartFinancialYear(DateTime.Today);
            if (BalanceDate < sFY)
            {
                ModelState.TryAddModelError("BalanceDate", $"Balance date cannot be before the start of the financial year {sFY:d}");
            }
            if (!ModelState.IsValid)
            {
                await FillUpLists();
                return Page();
            }
            // ^^^^ model validation end ^^^^
            //
            // --- contract and financial accounts ---
            await RecordContractData();
            // --- account opening transaction ---
            string st = await RecordInitialTransaction(BalanceDate);
            if (st.Length > 0)
            {
                TempData["ErrorMessage"] = st;
                await FillUpLists();
                return Page();
            }
            // --- contract plan transactions ---
            RecordContractPlanTransactions();
            // record updates to the database
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int x = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Contract {MainContract.ContractName} has been successfuly created! \n A total of {x} records inserted." +
                    $"\n Contract account number is {MainContract.IdFinAccount}";
            }
            catch (DbException ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error during saving to the database. \n {ex.Message}";
            }
            return RedirectToPage("../IndexSpecialContracts");
        }

        private async Task RecordContractData()
        {
            IConfiguration sectionCategory = _configuration.GetSection("AccountCategoriesSettings");
            int accCateg = sectionCategory.GetValue<int>("InvestmentManagementFund");
            if (accCateg == 0) { accCateg = 1004; } //guard against misreading of value 
            //get contract idfinaccount
            FinAccountHelper fah = new(_context, _configuration);
            //update contract data
            int[] tempids = await fah.NewIdAccountByCateg(accCateg);
            MainContract.IdFinAccount = tempids[0];
            MainContract.ContractType = (short)CategoryContract;
            MainContract.IsActiveContract = true;
            // create and add contract account
            FinAccount cfa = new()
            {
                IdFinAccount = MainContract.IdFinAccount,
                Name = MainContract.ContractName,
                AccountCategory = (short)CategoryContract,
                AccountOwner = MainContract.IdContractor,
                IsLinked = true
            };
            _context.FinAccounts.Add(cfa);
            // add contract supplement
            // investment fund fee accounts
            int ctgManagementFee = sectionCategory.GetValue<int>("FundManagementFee");
            ctgManagementFee = ctgManagementFee == 0 ? 5201 : ctgManagementFee;
            int ctgAdminFee = sectionCategory.GetValue<int>("FundAdminFee");
            ctgAdminFee = ctgAdminFee == 0 ? 5202 : ctgAdminFee; // default to 5202 if not set
            int ctgAdviserFee = sectionCategory.GetValue<int>("FundAdviserFee");
            ctgAdviserFee = ctgAdviserFee == 0 ? 5203 : ctgAdviserFee; // default to 5203 if not set
            int ctgAccountingFee = sectionCategory.GetValue<int>("FundAccountingFee");
            ctgAccountingFee = ctgAccountingFee == 0 ? 5206 : ctgAccountingFee; // default to 5206 if not set
            // capital gain account
            int ctgCapitalGain = sectionCategory.GetValue<int>("ProfitLoss");
            ctgCapitalGain = ctgCapitalGain == 0 ? 3001 : ctgCapitalGain; // default to 4108 if not set
            // create the account ids
            int idManagementFee = (await fah.NewIdAccountByCateg(ctgManagementFee))[0];
            int idAdminFee = (await fah.NewIdAccountByCateg(ctgAdminFee))[0];
            int idAdviserFee = (await fah.NewIdAccountByCateg(ctgAdviserFee))[0];
            int idAccountingFee = (await fah.NewIdAccountByCateg(ctgAccountingFee))[0];
            int idCapitalGain = (await fah.NewIdAccountByCateg(ctgCapitalGain))[0];
            FinAccount ManagementFee = new()
            {
                IdFinAccount = idManagementFee,
                Name = "Fund management fee",
                AccountCategory = (short)ctgManagementFee,
                AccountOwner = MainContract.IdContractor,
                IsLinked = true
            };
            _context.FinAccounts.Add(ManagementFee);
            FinAccount AdminFee = new()
            {
                IdFinAccount = idAdminFee,
                Name = "Fund administration fee",
                AccountCategory = (short)ctgAdminFee,
                AccountOwner = MainContract.IdContractor,
                IsLinked = true
            };
            _context.FinAccounts.Add(AdminFee);
            FinAccount AdviserFee = new()
            {
                IdFinAccount = idAdviserFee,
                Name = "Fund adviser fee",
                AccountCategory = (short)ctgAdviserFee,
                AccountOwner = MainContract.IdContractor,
                IsLinked = true
            };
            _context.FinAccounts.Add(AdviserFee);
            FinAccount AccountingFee = new()
            {
                IdFinAccount = idAccountingFee,
                Name = "Fund accounting fee",
                AccountCategory = (short)ctgAccountingFee,
                AccountOwner = MainContract.IdContractor,
                IsLinked = true
            };
            _context.FinAccounts.Add(AccountingFee);
            FinAccount CapitalGain = new()
            {
                IdFinAccount = idCapitalGain,
                Name = "Fund capital gain",
                AccountCategory = (short)ctgCapitalGain,
                AccountOwner = MainContract.IdContractor,
                IsLinked = true
            };
            _context.FinAccounts.Add(CapitalGain);
            // create the Investment Fund contract supplement
            MyInvestFund.IdInvestFund = MainContract.IdFinContract;
            MyInvestFund.IdCapitalGainAcc = idCapitalGain;
            MyInvestFund.IdManageAcc = idManagementFee;
            MyInvestFund.IdAdminAcc = idAdminFee;
            MyInvestFund.IdAccountingAcc = idAccountingFee;
            _context.InvestFunds.Add(MyInvestFund);
            _context.FinContracts.Add(MainContract);
            return;
        }

        private async Task<string> RecordInitialTransaction(DateTime transacDate)
        {
            // recording is done only if the InitialAmount differs from the Purchase balance + Cash balance
            // and balance date is within current financial year
            // get user account id
            var uaid = await _context.UserContracts.Where(p => p.IdUser == MainContract.IdContractor).Select(s => s.UserAccountID).FirstOrDefaultAsync();
            if (uaid <= 0) { return "Error in retrieving the user dara"; }
            string detailOpen = _configuration.GetSection("TransactionDetail").GetValue<string>("Open") ?? "Open account ";
            PlanTransaction fin = new()
            {
                IdTransaction = GeneratorID.Int64IDBasedOnDate(transacDate),
                TransactionDate = transacDate,
                DebitAccount = MainContract.IdFinAccount,
                CreditAccount = uaid,
                TransactionValue = PurchaseBalance,
                TransactionDetail = detailOpen,
                ContractIdentifier = MainContract.IdFinAccount,
                ExecutedTransaction = false,
                IdSource = uaid
            };
            _context.PlanTransactions.Add(fin);
            return string.Empty;
        }

        private void RecordContractPlanTransactions()
        {
            //get the enddate for plantransactions
            CalculatorDates cada = new(MainContract.IdFrequency);
            DateTime firstInvoice = cada.NextCycleDate(MainContract.InvoiceDate, DateTime.Today);
            GeneralHelper gh = new(_configuration);
            DateTime endDatePT = gh.EndPeriodPlanTransaction(firstInvoice, MainContract.EndDate, MainContract.IdEndAction);
            //get user tax account id
            int tnt = cada.NumberCyclesPerPeriod(firstInvoice, endDatePT);
            // create the plan transactions for the contract
            for (int i = 0; i < tnt; i++)
            {
                // management fee
                PlanTransaction mgmtFee = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(firstInvoice),
                    TransactionDate = firstInvoice,
                    DebitAccount = MyInvestFund.IdManageAcc,
                    CreditAccount = MainContract.IdFinAccount,
                    TransactionValue = MyInvestFund.ManagementFee,
                    TransactionDetail = "Management fee",
                    ContractIdentifier = MainContract.IdFinAccount,
                    ExecutedTransaction = false,
                    IdSource = MainContract.IdFinAccount
                };
                _context.PlanTransactions.Add(mgmtFee);
                // administration fee
                PlanTransaction adminFee = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(firstInvoice),
                    TransactionDate = firstInvoice,
                    DebitAccount = MyInvestFund.IdAdminAcc,
                    CreditAccount = MainContract.IdFinAccount,
                    TransactionValue = MyInvestFund.AdminFee,
                    TransactionDetail = "Administration fee",
                    ContractIdentifier = MainContract.IdFinAccount,
                    ExecutedTransaction = false,
                    IdSource = MainContract.IdFinAccount
                };
                _context.PlanTransactions.Add(adminFee);
                // adviser fee
                PlanTransaction adviserFee = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(firstInvoice),
                    TransactionDate = firstInvoice,
                    DebitAccount = MyInvestFund.IdAdviseAcc,
                    CreditAccount = MainContract.IdFinAccount,
                    TransactionValue = MyInvestFund.AdviserFee,
                    TransactionDetail = "Adviser fee",
                    ContractIdentifier = MainContract.IdFinAccount,
                    ExecutedTransaction = false,
                    IdSource = MainContract.IdFinAccount
                };
                _context.PlanTransactions.Add(adviserFee);
                if (MyInvestFund.AccountingFee > 0)
                {
                    // accounting fee
                    PlanTransaction accountingFee = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(firstInvoice),
                        TransactionDate = firstInvoice,
                        DebitAccount = MyInvestFund.IdAccountingAcc,
                        CreditAccount = MainContract.IdFinAccount,
                        TransactionValue = MyInvestFund.AccountingFee,
                        TransactionDetail = "Accounting fee",
                        ContractIdentifier = MainContract.IdFinAccount,
                        ExecutedTransaction = false,
                        IdSource = MainContract.IdFinAccount
                    };
                    _context.PlanTransactions.Add(accountingFee);
                }
                // increment date for next cycle
                firstInvoice = cada.IncrementCycleDate(firstInvoice, false);
            }
            return;
        }
    }
}
