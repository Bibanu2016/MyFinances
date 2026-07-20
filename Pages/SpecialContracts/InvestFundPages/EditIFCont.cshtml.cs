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
    public class EditIFContModel(AppDBContext context, IConfiguration configuration, IMemoryCache memoryCache, IComboFillService comboFill) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly IComboFillService _comboFill = comboFill;

        // contract type selection variable
        [BindProperty, DataType(DataType.Currency), Range(0, 1000000000, ErrorMessage = "Value {0} is required in the range {1} to {2}"), Display(Name = "Current balance")]
        public decimal PurchaseBalance { get; set; } = 0;
        [BindProperty, DataType(DataType.DateTime), Display(Name = "Date of current balance")]
        public DateTime BalanceDate { get; set; } = DateTime.Now;

        // Combobox source data
        public SelectList Frequency { get; set; } = default!;
        public SelectList Organisation { get; set; } = default!;
        public SelectList Users { get; set; } = default!;
        // Contract data
        [BindProperty]
        public FinContract MainContract { get; set; } = default!;
        [BindProperty]
        public InvestFund MyInvestFund { get; set; } = default!;

        [BindProperty]
        public bool IsExecuted { get; set; } = false;


        public async Task<IActionResult> OnGet(string id)
        {
            // Load the contract data from the database
            Guid gid = Guid.Parse(id);
            var mc = await _context.FinContracts.FindAsync(gid);
            var ifc = await _context.InvestFunds.FindAsync(gid);
            if(mc == null || ifc == null) { return NotFound(); }
            MainContract = mc;
            MyInvestFund = ifc;
            FinContractHelper fch = new(_context, _configuration);
            IsExecuted = await fch.GetContractExecutionState(MainContract.IdFinAccount);
            CalculatorDates cda = new(MainContract.IdFrequency);
            MainContract.InvoiceDate = cda.NextCycleDate(MainContract.InvoiceDate, DateTime.Today);
            // Load combobox data
            await FillUpLists();
            ViewData["Title"] = $"Edit Investment Fund Contract {MainContract.ContractName}";
            return Page();
        }

        // --- Procedures to create the lists ---
        /// <summary>
        /// Populates the lists with the data from the database
        /// </summary>
        /// <returns></returns>
        private async Task FillUpLists()
        {
            Frequency = await _comboFill.GetPaymentFrequencyList();
            Organisation = await _comboFill.GetOrganisationsList(6);
            Users = await _comboFill.GetUsersList();
            await GetInitialBalance();
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
            CustomValidator(sFY);
            if (!ModelState.IsValid)
            {
                await FillUpLists();
                return Page();
            }
            // ^^^^ model validation end ^^^^
            //
            // --- get the original values for the investment fund contract ---
            var mc = await _context.FinContracts.AsNoTracking().FirstOrDefaultAsync(c => c.IdFinContract == MainContract.IdFinContract);
            var ifc = await _context.InvestFunds.AsNoTracking().FirstOrDefaultAsync(i => i.IdInvestFund == MainContract.IdFinContract);
            if (mc == null || ifc == null) { return RedirectToAction("Get"); }
            FinContract OriginalMain = mc;
            InvestFund OriginalInvest = ifc;
            // --- update contract and financial accounts ---
            if(!MainContract.Equals(OriginalMain))
            {
                _context.FinContracts.Update(MainContract);
            }
            if(!MyInvestFund.Equals(OriginalInvest))
            {
                _context.InvestFunds.Update(MyInvestFund);
            }
            // ---determine if the transactions need to be replaced ---
            bool replaceTransactions = ReplacePlanTransactions(MainContract, OriginalMain, sFY);
            if(replaceTransactions)
            {
                // delete all existing contract plan transactions
                await DeletePlanTransactions();
                // create new plan transactions
                string result = await RecordInitialTransaction(BalanceDate);
                if (!string.IsNullOrEmpty(result)) { TempData["ErrorMessage"] = result; return RedirectToPage("../IndexSpecialContracts"); }
                RecordContractPlanTransactions();
            }
            else
            {
                // update the initial transaction if there is a change in the balance date
                var userFA = await _context.UserContracts.Where(p => p.IdUser == MainContract.IdContractor).Select(s => s.UserAccountID).FirstOrDefaultAsync();
                var ipt = await GetInitialTransaction(MainContract.IdFinAccount, userFA);
                if(ipt.Count > 0)
                {
                    if (ipt[0].TransactionDate != BalanceDate || ipt[0].TransactionValue != PurchaseBalance)
                    {
                        ipt[0].TransactionDate = BalanceDate;
                        ipt[0].TransactionValue = PurchaseBalance;
                        _context.PlanTransactions.Attach(ipt[0]).State = EntityState.Modified;
                    }                
                }
                // update the transactions if there is any changes in the amounts payable as per the InvestAccount
                if(MyInvestFund.AdviserFee != OriginalInvest.AdviserFee)
                {
                    var adviserPTL = await _context.PlanTransactions.Where(p => p.ContractIdentifier == MainContract.IdFinAccount && p.DebitAccount == MyInvestFund.IdAdviseAcc && p.TransactionDate >= DateTime.Today).ToListAsync();
                    if(adviserPTL.Count > 0)
                    {
                        foreach(PlanTransaction pt in adviserPTL) { pt.TransactionValue = MyInvestFund.AdviserFee; _context.PlanTransactions.Update(pt); }
                    }
                }
                if(MyInvestFund.AdminFee != OriginalInvest.AdminFee)
                {
                    var adminPTL = await _context.PlanTransactions.Where(p => p.ContractIdentifier == MainContract.IdFinAccount && p.DebitAccount == MyInvestFund.IdAdminAcc && p.TransactionDate >= DateTime.Today).ToListAsync();
                    if(adminPTL.Count > 0)
                    {
                        foreach(PlanTransaction pt in adminPTL) { pt.TransactionValue = MyInvestFund.AdminFee; _context.PlanTransactions.Update(pt); }
                    }
                }
                if (MyInvestFund.ManagementFee != OriginalInvest.ManagementFee)
                {
                    var managePTL = await _context.PlanTransactions.Where(p => p.ContractIdentifier == MainContract.IdFinAccount && p.DebitAccount == MyInvestFund.IdManageAcc && p.TransactionDate >= DateTime.Today).ToListAsync();
                    if (managePTL.Count > 0)
                    {
                        foreach (PlanTransaction pt in managePTL) { pt.TransactionValue = MyInvestFund.ManagementFee; _context.PlanTransactions.Update(pt); }
                    }
                }
                if (MyInvestFund.AccountingFee != OriginalInvest.AccountingFee)
                {
                    var accountingPTL = await _context.PlanTransactions.Where(p => p.ContractIdentifier == MainContract.IdFinAccount && p.DebitAccount == MyInvestFund.IdAccountingAcc && p.TransactionDate >= DateTime.Today).ToListAsync();
                    if (accountingPTL.Count > 0)
                    {
                        foreach (PlanTransaction pt in accountingPTL) { pt.TransactionValue = MyInvestFund.AccountingFee; _context.PlanTransactions.Update(pt); }
                    }
                }
            }
            // record updates to the database
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int x = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Contract {MainContract.ContractName} has been successfuly updated! \n A total of {x} records updated.";
            }
            catch (DbException ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error during saving to the database. \n {ex.Message}";
            }
            return RedirectToPage("../IndexSpecialContracts");
        }

        private async Task GetInitialBalance()
        {
            var ufa = await _context.UserContracts.Where(p=>p.IdUser == MainContract.IdContractor).Select(s=>s.UserAccountID).FirstOrDefaultAsync();
            var fpt = await _context.PlanTransactions.Where(p=>p.DebitAccount == MainContract.IdFinAccount && p.CreditAccount == ufa).AsNoTracking().FirstOrDefaultAsync();
            if (fpt != null)
            {
                PurchaseBalance = fpt.TransactionValue;
                BalanceDate = fpt.TransactionDate;
            }
            return;
        }

        private void CustomValidator(DateTime startFY)
        {
            if (BalanceDate < startFY)
            {
                ModelState.TryAddModelError("BalanceDate", $"Balance date cannot be before the start of the financial year {startFY:d}");
            }
            if (BalanceDate < MainContract.StartDate)
            {
                ModelState.TryAddModelError("BalanceDate", $"Balance date cannot be before the start of the contract {MainContract.StartDate:d}");
            }
            if (BalanceDate > DateTime.Today)
            {
                ModelState.TryAddModelError("BalanceDate", $"Balance date cannot be after {DateTime.Today:d}");
            }
            if(MainContract.InvoiceDate < DateTime.Today)
            {
                ModelState.TryAddModelError("InvoiceDate", $"Invoice date cannot be before {DateTime.Today:d}");
            }
            return;
        }

        private async Task DeletePlanTransactions()
        {
            var cpt = await _context.PlanTransactions.Where(p => p.ContractIdentifier == MainContract.IdFinAccount).ToListAsync();
            foreach(PlanTransaction pt in cpt)
            {
                _context.PlanTransactions.Remove(pt);
            }
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

        private async Task<List<PlanTransaction>> GetInitialTransaction(int debitAcc, int creditAcc)
        {
            return await _context.PlanTransactions.Where(p => p.DebitAccount == debitAcc && p.CreditAccount == creditAcc).OrderBy(o=>o.TransactionDate).AsNoTracking().ToListAsync(); 
        }

        private bool ReplacePlanTransactions(FinContract mainCont, FinContract origCont, DateTime startFinYear)
        {
            CalculatorDates cda = new(origCont.IdFrequency);
            DateTime origInvoice = cda.NextCycleDate(origCont.InvoiceDate, DateTime.Today);
            int x = 1;
            if(mainCont.IdFrequency != origCont.IdFrequency) { x = 0; }
            if(mainCont.InvoiceDate.Date != origInvoice.Date) { x = 0; }
            if(mainCont.StartDate > startFinYear && (mainCont.StartDate.Date != origCont.StartDate.Date))
            {
                x = 0;
            }
            return x == 0;
        }


    }
}
