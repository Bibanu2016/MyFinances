using Azure;
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
using MyFinances.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace MyFinances.Pages.Contracts
{
    public class InvestFundContractAddModel(AppDBContext context, IConfiguration configuration, IMemoryCache memoryCache, IComboFillService comboFill) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly IComboFillService _comboFill = comboFill;
        //
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
        public SelectList ContractCategory { get; set; } = default!;
        //
        // Contract data
        [BindProperty]
        public FinContract MainContract { get; set; } = default!;
        [BindProperty]
        public InvestFund MyInvestFund { get; set; } = default!;


        public async Task OnGet()
        {
            MainContract = new FinContract() { IdFinContract = new Guid()};
            MyInvestFund = new InvestFund() { IdInvestFund = MainContract.IdFinContract };
            ContractCategory = await _comboFill.GetAccountCategoryList(8);
            ViewData["Title"] = "Create Investment Fund Contract";
            await FillUpLists();
        }

        // --- Procedures to create the lists ---
        /// <summary>
        /// Populates the lists with the data from the database
        /// </summary>
        /// <returns></returns>
        private async Task FillUpLists()
        {
            Frequency = await _comboFill.GetPaymentFrequencyList();
            Organisation = await _comboFill.GetOrganisationsList();
            Users = await _comboFill.GetUsersList();
            return;
        }

        //
        // POST Method
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // --- model validation start ---
            CustomValidatorContract();
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
            CreateIFPlanTransacs();
            MainContract.EquivMonthlyRate = 0;
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
            return RedirectToPage("./ContractsList");
        }
        private void CustomValidatorContract()
        {
            // balance day less than today
            if (BalanceDate > DateTime.Today) { this.ModelState.TryAddModelError("BalanceDate", "A date earlier than or today is required"); }
            // super value less than gross value
            if (BalanceDate < MainContract.StartDate) { this.ModelState.TryAddModelError("BalanceDate", "A date later than or the contact Start Date is required"); }
            CalculatorDates calculatorDates = new(MainContract.IdFrequency);
            DateTime lastPayment = calculatorDates.LastCycleDate(MainContract.InvoiceDate, DateTime.Now);
            if (BalanceDate < lastPayment) { this.ModelState.TryAddModelError("BalanceDate", $"A date later than last peyment date of {lastPayment} is required."); }
            return;
        }


        private async Task RecordContractData()
        {
            //get contract idfinaccount
                
            FinAccountHelper fah = new(_context, _configuration);
            //update contract data
            int[] tempids = await fah.NewIdAccountByCateg(CategoryContract);
            MainContract.IdFinAccount = tempids[0];
            MainContract.ContractType = 8;
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
            IConfiguration sectionDetail = _configuration.GetSection("TransactionDetail");
            IConfiguration sectionCategory = _configuration.GetSection("AccountCategoriesSettings");
            int[] investAccounts = [0, 0, 0, 0, 0, 0];
            string[] investNames = ["ProfitLoss", "FundAdviserFee", "FundManagementFee", "FundAdminFee", "FundAccountingFee", "TaxPaid"];
            string[] nameAccounts = ["Profit/Loss", "Fund Adviser Fee", "Fund Management Fee", "Fund Admin Fee", "Fund Accounting Fee", "Tax Paid"];
            for (int i = 0; i < investAccounts.Length; i++)
            {
                investAccounts[i] = await fah.NewIdAccountByCateg(sectionCategory.GetValue<int>(investNames[i])).ContinueWith(t => t.Result[0]);
                FinAccount cfaInvest = new()
                {
                    IdFinAccount = investAccounts[i],
                    Name = $"{nameAccounts[i]} - {MainContract.ContractName}",
                    AccountCategory = (short)sectionCategory.GetValue<int>(investNames[i]),
                    AccountOwner = MainContract.IdContractor,
                    IsLinked = true
                };
                _context.FinAccounts.Add(cfaInvest);
            };
            _context.FinContracts.Add(MainContract);
            return;
        }

        private async Task<string> RecordInitialTransaction(DateTime transacDate)
        {
            // recording is done only if the InitialAmount differs from the Purchase balance + Cash balance
            // and balance date is within current financial year
            // get user account id
            var uaid = await _context.UserContracts.Where(p => p.IdUser == MainContract.IdContractor).Select(s => s.UserAccountID).FirstOrDefaultAsync();
            if(uaid == 0)
            {
                return $"User account for user {MainContract.IdContractor} not found!";
            }
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

        private void CreateIFPlanTransacs()
        {
            //get the enddate for plantransactions
            CalculatorDates cada = new(MainContract.IdFrequency);
            DateTime firstInvoice = cada.NextCycleDate(MainContract.InvoiceDate, DateTime.Now);
            GeneralHelper gh = new(_configuration);
            DateTime endDatePT = gh.EndPeriodPlanTransaction(firstInvoice, DateTime.Parse("2100-12-31"), 2);
            int tnt = cada.NumberCyclesPerPeriod(firstInvoice, endDatePT);
            IConfigurationSection section = _configuration.GetSection("TransactionDetail");
            var detailFee = section.GetValue<string>("FeeOut") ?? "Fess to ";
            for (int i = 0; i < tnt; i++)
            {
                PlanTransaction pt_1 = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(firstInvoice),
                    TransactionDate = firstInvoice,
                    DebitAccount = MyInvestFund.IdAdviseAcc,
                    CreditAccount = MainContract.IdFinAccount,
                    TransactionValue = MyInvestFund.AdviserFee,
                    TransactionDetail = detailFee,
                    ContractIdentifier = MainContract.IdFinAccount,
                    ExecutedTransaction = false,
                    IdSource = MainContract.IdFinAccount
                };
                _context.PlanTransactions.Add(pt_1);
                PlanTransaction pt_2 = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(firstInvoice),
                    TransactionDate = firstInvoice,
                    DebitAccount = MyInvestFund.IdManageAcc,
                    CreditAccount = MainContract.IdFinAccount,
                    TransactionValue = MyInvestFund.ManagementFee,
                    TransactionDetail = detailFee,
                    ContractIdentifier = MainContract.IdFinAccount,
                    ExecutedTransaction = false,
                    IdSource = MainContract.IdFinAccount
                };
                _context.PlanTransactions.Add(pt_2);
                PlanTransaction pt_3 = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(firstInvoice),
                    TransactionDate = firstInvoice,
                    DebitAccount = MyInvestFund.IdAdminAcc,
                    CreditAccount = MainContract.IdFinAccount,
                    TransactionValue = MyInvestFund.AdminFee,
                    TransactionDetail = detailFee,
                    ContractIdentifier = MainContract.IdFinAccount,
                    ExecutedTransaction = false,
                    IdSource = MainContract.IdFinAccount
                };
                _context.PlanTransactions.Add(pt_3);
                if(MyInvestFund.AccountingFee > 0)
                {
                    PlanTransaction pt_4 = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(firstInvoice),
                        TransactionDate = firstInvoice,
                        DebitAccount = MyInvestFund.IdAccountingAcc,
                        CreditAccount = MainContract.IdFinAccount,
                        TransactionValue = MyInvestFund.AccountingFee,
                        TransactionDetail = detailFee,
                        ContractIdentifier = MainContract.IdFinAccount,
                        ExecutedTransaction = false,
                        IdSource = MainContract.IdFinAccount
                    };
                    _context.PlanTransactions.Add(pt_4);
                }
                firstInvoice = cada.IncrementCycleDate(firstInvoice);
            }
            return;
        }


    }
}
