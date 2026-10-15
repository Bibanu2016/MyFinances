using FinanceCalculator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using MyFinances.ViewModels;
using System.Data;
using System.Data.Common;

namespace MyFinances.Pages.Contracts
{
    public class ContractEditModel(AppDBContext context, IConfiguration config, IComboFillService fillService) : PageModel
    {
        private readonly AppDBContext _context = context;
        public readonly IConfiguration _config = config;
        private readonly IComboFillService _comboFill = fillService;

        [BindProperty]
        public ViewModels.ContractFull CFM { get; set; } = default!;

        [BindProperty]
        public RealBalance LastRealBalance { get; set; } = default!;

        [BindProperty]
        public RealBalance LastPlanBalance { get; set; } = default!;
        [BindProperty]
        public decimal RealBalance { get; set; } = 0;
        [BindProperty]
        public decimal ForecastBalance { get; set; } = 0;
        [BindProperty]
        public DateTime BalanceDate { get; set; } = DateTime.Now;
        [BindProperty]
        public decimal CardCashRealBalance { get; set; } = 0;
        [BindProperty]
        public decimal CardCashPlanBalance { get; set; } = 0;

        public decimal OriginalForeBalance { get; set; } = 0;
        public decimal OriginalCashBalance { get; set; } = 0;

        private int EquivMonthRate { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        private Guid Id { get; set; } = Guid.Empty;
        [BindProperty]
        public bool BalanceUpdateOK { get; set; } = false;
        [BindProperty]

        public short RestrictionsOn { get; set; } = 0;

        public ContractFull OriginalContract { get; set; } = default!;

        #region COMBO_LISTS
        //combo lists

        public SelectList Frequency { get; set; } = default!;
        public SelectList EndAction { get; set; } = default!;
        public SelectList Organisation { get; set; } = default!;
        public SelectList Payable { get; set; } = default!;
        public SelectList Bank { get; set; } = default!;
        public SelectList Users { get; set; } = default!;
        public SelectList TangibleAsset { get; set; } = default!;
        public SelectList SuperAccount { get; set; } = default!;
        // end combo lists
        #endregion COMBO_LISTS


        public string FrequencyName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(Guid? pid)
        {
            if (pid == null)
            {
                return NotFound();
            }
            Id = pid ?? Guid.Empty;
            FinContractHelper fch = new(_context, _config);
            CFM = await fch.GetContractOnID(Id);
            if (CFM is null) { return NotFound(); }
            CFM.FinContract.InvoiceDate = new FinanceCalculator.CalculatorDates(CFM.FinContract.IdFrequency).NextCycleDate(CFM.FinContract.InvoiceDate, DateTime.Today);
            OriginalContract = CFM;
            await FillUpLists();
            if (CFM.FinContract.ContractType == 2) { TangibleAsset = await _comboFill.GetContractEndActionList(); }
            if (CFM.FinContract.ContractType == 4) { SuperAccount = await _comboFill.GetSuperFinAccountsList(); }
            // get contract balance - apply only for Deposit, Loan, and CreditCard
            if (CFM.FinContract.ContractType < 3)
            {
                RealBalance = await fch.GetContractRealBalance(CFM.FinContract.IdFinAccount, DateTime.Now);
                ForecastBalance = await fch.GetContractPlannedBalance(CFM.FinContract.IdFinAccount, DateTime.Now);
                OriginalForeBalance = ForecastBalance;
                if (CFM.FinContract.ContractType == 3)
                {
                    CardCashRealBalance = await fch.GetContractRealBalance(CFM.FinContract.IdFinAccount, DateTime.Now, CFM.CreditCard.CashOutAccount);
                    CardCashPlanBalance = await fch.GetContractPlannedBalance(CFM.FinContract.IdFinAccount, DateTime.Now, CFM.CreditCard.CashOutAccount);
                    OriginalCashBalance = CardCashPlanBalance;
                }
            }
            bool es = await fch.GetContractExecutionState(CFM.FinContract.IdFinAccount);
            if (es) { RestrictionsOn = 1; }
            return Page();
        }

        private async Task FillUpLists()
        {
            Frequency = await _comboFill.GetPaymentFrequencyList();
            EndAction = await _comboFill.GetContractEndActionList();
            Organisation = await _comboFill.GetOrganisationsList();
            Payable = await _comboFill.GetPayableAccountsList();
            Bank = await _comboFill.GetNTAssetAccountsList();
            Users = await _comboFill.GetUsersList();
            return;
        }


        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            TailorValidationModel(CFM.FinContract.ContractType);
            //ContractCustomValidator ccv = new(CFN.ContractFull, this.ModelState);
            //ccv.MainContractCustomValidator();
            if (!ModelState.IsValid)
            {
                return Page();
            }
            // do not update if the loan start date is earlier than the start of the FY
            //
            FinContractHelper fch = new(_context, _config);
            OriginalContract = await fch.GetContractOnID(Id);
            if (OriginalContract == default) { TempData["ErrorMessage"] = $"Error in retrieving data for contract id {Id}"; return RedirectToPage("./ContractsList"); }
            //
            // update FinContract if there are changes in data
            //
            bool changesMade = UpdateContractData(OriginalContract);
            //
            // if name changed update FinAccount
            //
            if (changesMade == false) { TempData["ErrorMessage"] = $"Error in updating contract data for contract id {Id} \n or no changes have been detected."; return RedirectToPage("./ContractsList"); }
            string errMessage = string.Empty;
            if (CFM.FinContract.ContractName != OriginalContract.FinContract.ContractName)
            {
                errMessage = await UpdateAccountsName(OriginalContract.FinContract.ContractName, CFM.FinContract.ContractName);
            }
            if (errMessage.Length > 0)
            {
                TempData["ErrorMessage"] = errMessage;
                return RedirectToPage("./ContractsList");
            }
            /*
             * Update forecast transactions
             * 
             */
            bool cIsEx = await fch.GetContractExecutionState(Id);
            string success = string.Empty;
            int ptAction = ReplaceOrUpdate(cIsEx, CFM, OriginalContract);
            DateTime fromDate = await fch.GetDateLastTransac(cIsEx, CFM.FinContract.IdFinAccount, DateTime.Now);
            if (ptAction == 1) { success = await ReplacePlanTransactions(fromDate); }
            if (ptAction == -1) { success = await UpdateContractPlanTransactions(fromDate); }
            // update contract equivalent monthly rate
            CFM.FinContract.EquivMonthlyRate = EquivMonthRate;
            _context.FinContracts.Update(CFM.FinContract);
            if (success.Length > 0)
            {
                TempData["ErrorMessage"] = success;
                return RedirectToPage("./ContractsList");
            }
            // record updates to the database
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                int x = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Contract with id = {Id} has been successfuly updated! \n A total of {x} records affected.";
            }
            catch (DbException ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error during saving to the database. \n {ex.Message}";
            }
            return RedirectToPage("./ContractsList");
        }

        /// <summary>
        /// Removes non required classes from the ContractFull from validation model
        /// </summary>
        /// <param name="cc">Contract type index</param>
        private void TailorValidationModel(int cc)
        {
            ModelStateDictionary ms = this.ModelState;
            string[] supClass =
                [
                    "CFM.Deposit",
                    "CFM.Loan",
                    "CFM.CreditCard",
                    "CFM.Employment",
                    "CFM.Income",
                    "CFM.Bill"
                ];
            switch (cc)
            {
                case 1:
                    ms.Remove(supClass[1]);
                    ms.Remove(supClass[2]);
                    ms.Remove(supClass[3]);
                    ms.Remove(supClass[4]);
                    ms.Remove(supClass[5]);
                    break;
                case 2:
                    ms.Remove(supClass[0]);
                    ms.Remove(supClass[2]);
                    ms.Remove(supClass[3]);
                    ms.Remove(supClass[4]);
                    ms.Remove(supClass[5]);
                    break;
                case 3:
                    ms.Remove(supClass[0]);
                    ms.Remove(supClass[1]);
                    ms.Remove(supClass[3]);
                    ms.Remove(supClass[4]);
                    ms.Remove(supClass[5]);
                    break;
                case 4:
                    ms.Remove(supClass[0]);
                    ms.Remove(supClass[1]);
                    ms.Remove(supClass[2]);
                    ms.Remove(supClass[4]);
                    ms.Remove(supClass[5]);
                    break;
                case 5:
                    ms.Remove(supClass[0]);
                    ms.Remove(supClass[1]);
                    ms.Remove(supClass[2]);
                    ms.Remove(supClass[3]);
                    ms.Remove(supClass[5]);
                    break;
                case 6:
                    ms.Remove(supClass[0]);
                    ms.Remove(supClass[1]);
                    ms.Remove(supClass[2]);
                    ms.Remove(supClass[3]);
                    ms.Remove(supClass[4]);
                    break;
                case 7:
                    ms.Remove(supClass[0]);
                    ms.Remove(supClass[1]);
                    ms.Remove(supClass[2]);
                    ms.Remove(supClass[3]);
                    ms.Remove(supClass[4]);
                    break;
                default:
                    ms.Remove(supClass[0]);
                    ms.Remove(supClass[1]);
                    ms.Remove(supClass[2]);
                    ms.Remove(supClass[3]);
                    ms.Remove(supClass[4]);
                    ms.Remove(supClass[5]);
                    break;
            }

        }
        #region updateprocedures

        private static int ReplaceOrUpdate(bool isContractExecuted, ContractFull modCont, ContractFull origCont)
        {
            // +1 - Update; 0 - No change; -1 - Replace 
            int result = 0;
            int rC = 0;
            int uC = 0;
            if (isContractExecuted)
            {
                if (modCont.FinContract.InvoiceDate.Date != origCont.FinContract.InvoiceDate.Date) { rC++; }
                if (modCont.FinContract.InvoiceNetDays != origCont.FinContract.InvoiceNetDays) { rC++; }
                if (modCont.FinContract.IdPayAccount != origCont.FinContract.IdPayAccount) { uC++; }
                if (modCont.FinContract.IdBankAccount != origCont.FinContract.IdBankAccount) { uC++; }
                switch (modCont.FinContract.ContractType)
                {
                    case 1:
                        if (modCont.Deposit.DepositRate != origCont.Deposit.DepositRate) { uC++; }
                        if (modCont.Deposit.InterestRatePerYear != origCont.Deposit.InterestRatePerYear) { uC++; }
                        break;
                    case 2:
                        if (modCont.Loan.LoanInterest != origCont.Loan.LoanInterest) { uC++; }
                        if (modCont.Loan.ExtraPay != origCont.Loan.ExtraPay) { uC++; }
                        if (modCont.Loan.LoanAmount != origCont.Loan.LoanAmount) { rC++; }
                        break;
                    case 3:
                        if (modCont.CreditCard.CreditCardPayPercent != origCont.CreditCard.CreditCardPayPercent) { uC++; }
                        if (modCont.CreditCard.CreditCardInterest != origCont.CreditCard.CreditCardInterest) { uC++; }
                        if (modCont.CreditCard.CashRate != origCont.CreditCard.CashRate) { uC++; }
                        if (modCont.CreditCard.CycleFee != origCont.CreditCard.CycleFee) { uC++; }
                        if (modCont.CreditCard.CreditCardInsuranceRate != origCont.CreditCard.CreditCardInsuranceRate) { uC++; }
                        if (modCont.CreditCard.ExtraPay != origCont.CreditCard.ExtraPay) { uC++; }
                        break;
                    case 4:
                        if (modCont.Employment.GrossPaidCycle != origCont.Employment.GrossPaidCycle) { uC++; }
                        if (modCont.Employment.TaxPaidCycle != origCont.Employment.TaxPaidCycle) { uC++; }
                        if (modCont.Employment.SuperPaidCycle != origCont.Employment.SuperPaidCycle) { uC++; }
                        if (modCont.Employment.IdSuperAccount != origCont.Employment.IdSuperAccount) { uC++; }
                        break;
                    case 5:
                        if (modCont.Income.GrossPerCycle != origCont.Income.GrossPerCycle) { uC++; }
                        if (modCont.Income.TaxPaidCycle != origCont.Income.TaxPaidCycle) { uC++; }
                        break;
                    case 6:
                        if (modCont.Bill.BillCycleRate != origCont.Bill.BillCycleRate) { uC++; }
                        break;
                    case 7:
                        if (modCont.Bill.BillCycleRate != origCont.Bill.BillCycleRate) { uC++; }
                        break;
                }
            }
            else
            {
                if (modCont.FinContract.StartDate.Date != origCont.FinContract.StartDate.Date) { rC++; }
                if (modCont.FinContract.EndDate.Date != origCont.FinContract.EndDate.Date) { rC++; }
                if (modCont.FinContract.IdEndAction != origCont.FinContract.IdEndAction) { rC++; }
                if (modCont.FinContract.InvoiceDate.Date != origCont.FinContract.InvoiceDate.Date) { rC++; }
                if (modCont.FinContract.InvoiceNetDays != origCont.FinContract.InvoiceNetDays) { rC++; }
                if (modCont.FinContract.IdPayAccount != origCont.FinContract.IdPayAccount) { uC++; }
                if (modCont.FinContract.IdBankAccount != origCont.FinContract.IdBankAccount) { uC++; }
                switch (modCont.FinContract.ContractType)
                {
                    case 1:
                        if (modCont.Deposit.DepositRate != origCont.Deposit.DepositRate) { uC++; }
                        if (modCont.Deposit.InterestRatePerYear != origCont.Deposit.InterestRatePerYear) { uC++; }
                        if (modCont.Deposit.DepositInitialBalance != origCont.Deposit.DepositInitialBalance) { uC++; }
                        break;
                    case 2:
                        if (modCont.Loan.LoanAmount != origCont.Loan.LoanAmount) { rC++; }
                        if (modCont.Loan.LoanInterest != origCont.Loan.LoanInterest) { uC++; }
                        if (modCont.Loan.ExtraPay != origCont.Loan.ExtraPay) { uC++; }
                        break;
                    case 3:
                        if (modCont.CreditCard.CreditCardPayPercent != origCont.CreditCard.CreditCardPayPercent) { uC++; }
                        if (modCont.CreditCard.CreditCardInterest != origCont.CreditCard.CreditCardInterest) { uC++; }
                        if (modCont.CreditCard.CashRate != origCont.CreditCard.CashRate) { uC++; }
                        if (modCont.CreditCard.CycleFee != origCont.CreditCard.CycleFee) { uC++; }
                        if (modCont.CreditCard.CreditCardInsuranceRate != origCont.CreditCard.CreditCardInsuranceRate) { uC++; }
                        if (modCont.CreditCard.ExtraPay != origCont.CreditCard.ExtraPay) { uC++; }
                        break;
                    case 4:
                        if (modCont.Employment.GrossPaidCycle != origCont.Employment.GrossPaidCycle) { uC++; }
                        if (modCont.Employment.TaxPaidCycle != origCont.Employment.TaxPaidCycle) { uC++; }
                        if (modCont.Employment.SuperPaidCycle != origCont.Employment.SuperPaidCycle) { uC++; }
                        if (modCont.Employment.IdSuperAccount != origCont.Employment.IdSuperAccount) { uC++; }
                        break;
                    case 5:
                        if (modCont.Income.GrossPerCycle != origCont.Income.GrossPerCycle) { uC++; }
                        if (modCont.Income.TaxPaidCycle != origCont.Income.TaxPaidCycle) { rC++; }
                        break;
                    case 6:
                        if (modCont.Bill.BillCycleRate != origCont.Bill.BillCycleRate) { uC++; }
                        break;
                    case 7:
                        if (modCont.Bill.BillCycleRate != origCont.Bill.BillCycleRate) { uC++; }
                        break;
                }
            }
            if (rC > 0) { result = 1; }
            else
            {
                if (uC > 0) { result = -1; }
            }
            return result;
        }

        private bool UpdateContractData(ContractFull original)
        {
            bool result = false;
            if (original is null) { return result; }
            if (!CFM.FinContract.Equals(original.FinContract)) { _context.FinContracts.Update(CFM.FinContract); result = true; }
            switch (CFM.FinContract.ContractType)
            {
                case 1:
                    if (!CFM.Deposit.Equals(original.Deposit))
                    {
                        _context.Attach(CFM.Deposit).State = EntityState.Modified;
                        result = result || true;
                    }
                    break;
                case 2:
                    if (!CFM.Loan.Equals(original.Loan))
                    {
                        _context.Attach(CFM.Loan).State = EntityState.Modified;
                        result = result || true;
                    }
                    break;
                case 3:
                    if (!CFM.CreditCard.Equals(original.CreditCard))
                    {
                        _context.Attach(CFM.CreditCard).State = EntityState.Modified;
                        result = result || true;
                    }
                    break;
                case 4:
                    if (!CFM.Employment.Equals(original.Employment))
                    {
                        _context.Attach(CFM.Employment).State = EntityState.Modified;
                        result = result || true;
                    }
                    break;
                case 5:
                    if (!CFM.Income.Equals(original.Income))
                    {
                        _context.Attach(CFM.Income).State = EntityState.Modified;
                        result = result || true;
                    }
                    break;
                case >= 6:
                    if (!CFM.Bill.Equals(original.Bill))
                    {
                        _context.Attach(CFM.Bill).State = EntityState.Modified;
                        result = result || true;
                    }
                    break;
                default:
                    break;
            }
            return result;
        }

        private async Task<string> UpdateAccountsName(string oldName, string newName)
        {
            string result = string.Empty;
            var mainAccount = await _context.FinAccounts.FirstOrDefaultAsync(x => x.IdFinAccount == CFM.FinContract.IdFinAccount);
            if (mainAccount == null)
            {
                return $"Error in retrieving account data for contract {oldName}";
            }
            mainAccount.Name = mainAccount.Name.Replace(oldName, newName);
            switch (CFM.FinContract.ContractType)
            {
                case 1:
                    var depositInterest = await _context.FinAccounts.FirstOrDefaultAsync(x => x.IdFinAccount == CFM.Deposit.InterestAccount);
                    if (depositInterest is null) { return $"Error in retrieving account data for contract {oldName}"; }
                    depositInterest.Name = depositInterest.Name.Replace(oldName, newName);
                    _context.FinAccounts.Update(depositInterest);
                    break;
                case 2:
                    var loanInterest = await _context.FinAccounts.FirstOrDefaultAsync(x => x.IdFinAccount == CFM.Loan.IdAccountInterest);
                    if (loanInterest is null) { return $"Error in retrieving account data for contract {oldName}"; }
                    loanInterest.Name = loanInterest.Name.Replace(oldName, newName);
                    _context.FinAccounts.Update(loanInterest);
                    break;
                case 3:
                    var cardInterest = await _context.FinAccounts.FirstOrDefaultAsync(x => x.IdFinAccount == CFM.CreditCard.InterestAccount);
                    if (cardInterest is null) { return $"Error in retrieving account data for contract {oldName}"; }
                    cardInterest.Name = cardInterest.Name.Replace(oldName, newName);
                    var cardFee = await _context.FinAccounts.FirstOrDefaultAsync(x => x.IdFinAccount == CFM.CreditCard.FeeAccount);
                    if (cardFee is null) { return $"Error in retrieving account data for contract {oldName}"; }
                    cardFee.Name = cardFee.Name.Replace(oldName, newName);
                    var cardCash = await _context.FinAccounts.FirstOrDefaultAsync(x => x.IdFinAccount == CFM.CreditCard.CashOutAccount);
                    if (cardCash is null) { return $"Error in retrieving account data for contract {oldName}"; }
                    cardCash.Name = cardCash.Name.Replace(oldName, newName);
                    var cardInsurance = await _context.FinAccounts.FirstOrDefaultAsync(x => x.IdFinAccount == CFM.CreditCard.InsuranceAccount);
                    if (cardInsurance is null) { return $"Error in retrieving account data for contract {oldName}"; }
                    cardInsurance.Name = cardInsurance.Name.Replace(oldName, newName);
                    _context.FinAccounts.Update(cardInterest);
                    _context.FinAccounts.Update(cardFee);
                    _context.FinAccounts.Update(cardCash);
                    _context.FinAccounts.Update(cardInsurance);
                    break;
            }
            _context.FinAccounts.Update(mainAccount);
            return result;
        }

        private async Task<string> UpdateDeposit(DateTime startDate)
        {
            // updates existing PlanTransaction records for the Deposit contract for a change in Rate or / and Interest
            // get list of plan transactions for the contract
            var clpt = await _context.PlanTransactions.AsQueryable().Where(c => c.ContractIdentifier == CFM.FinContract.IdFinAccount && c.TransactionDate >= startDate).OrderBy(d => d.TransactionDate).ToListAsync();
            if (clpt.Count > 0)
            {
                decimal lbv = ForecastBalance;
                if (RestrictionsOn == 1) { BalanceUpdateOK = true; } // for Executed contracts the update is done automatically
                if (BalanceUpdateOK) { lbv = RealBalance; }
                List<PlanTransaction> incomeRateList = [.. clpt.Where(p => p.DebitAccount == CFM.FinContract.IdFinAccount && p.CreditAccount == OriginalContract.FinContract.IdPayAccount && p.IdSource == CFM.FinContract.IdFinAccount)];
                List<PlanTransaction> interestList = [.. clpt.Where(p => p.DebitAccount == CFM.Deposit.InterestAccount && p.CreditAccount == CFM.FinContract.IdFinAccount)];
                List<PlanTransaction> paymentRateList = [.. clpt.Where(p => p.DebitAccount == CFM.FinContract.IdFinAccount && p.CreditAccount == OriginalContract.FinContract.IdPayAccount && p.IdSource == OriginalContract.FinContract.IdPayAccount)];
                // update payment account if changed
                if (CFM.FinContract.IdPayAccount != OriginalContract.FinContract.IdPayAccount)
                {
                    foreach (PlanTransaction transaction in incomeRateList)
                    {
                        transaction.CreditAccount = CFM.FinContract.IdPayAccount;
                    }
                    foreach (PlanTransaction payment in paymentRateList)
                    {
                        payment.CreditAccount = CFM.FinContract.IdPayAccount;
                        payment.IdSource = CFM.FinContract.IdPayAccount;
                    }
                }
                int nrTransac = Math.Min(incomeRateList.Count, Math.Min(interestList.Count, paymentRateList.Count)); // record transactions for the minimum number of transactions between rate, interest and payment
                bool rateChange = CFM.Deposit.DepositRate != OriginalContract.Deposit.DepositRate;
                CalculatorFinance calfin = new(CFM.Deposit.InterestRatePerYear, CFM.FinContract.IdFrequency);
                for (int i = 0; i < nrTransac; i++)
                {
                    if (rateChange)
                    {
                        incomeRateList[i].TransactionValue = CFM.Deposit.DepositRate;
                        paymentRateList[i].TransactionValue = CFM.Deposit.DepositRate;
                        _context.PlanTransactions.Attach(incomeRateList[i]).State = EntityState.Modified;
                        _context.PlanTransactions.Attach(paymentRateList[i]).State = EntityState.Modified;
                    }
                    interestList[i].TransactionValue = calfin.InterestValuePerCycle(lbv);
                    _context.PlanTransactions.Attach(interestList[i]).State = EntityState.Modified;
                    lbv += CFM.Deposit.DepositRate + interestList[i].TransactionValue;
                }
                // update value for the contract EMR
                FrequencyConversion fc = new(CFM.FinContract.IdFrequency);
                if (rateChange) { EquivMonthRate = -1 * fc.CycletoMonthConversion(CFM.Deposit.DepositRate); }
                return string.Empty;
            }
            else
            {
                return $"No planned transactions could be retrieved";
            }
        }

        private async Task<string> UpdateLoan(DateTime startDate)
        {
            // updates existing PlanTransaction records for the Loan contract for a change in Interest or ExtraPay
            // get list of plan transactions for the contract
            var clpt = await _context.PlanTransactions.AsQueryable().Where(c => c.ContractIdentifier == CFM.FinContract.IdFinAccount && c.TransactionDate >= startDate).OrderBy(d => d.TransactionDate).ToListAsync();
            if (clpt.Count == 0) { return $"No plan transactions could be retrieved"; }
            List<PlanTransaction> pTrans = [.. clpt];
            decimal lbv = ForecastBalance;
            if (RestrictionsOn == 1)
            {
                lbv = RealBalance;
            }
            else
            {
                if (BalanceUpdateOK) { lbv = RealBalance; }
            }
            // build the lists for income payment, payment rate, interest charge, cover payment (if exists if not create cover payment)
            //
            List<PlanTransaction> incomeRateList = [.. clpt.Where(p => p.DebitAccount == CFM.FinContract.IdFinAccount && p.CreditAccount == OriginalContract.FinContract.IdPayAccount && p.IdSource == CFM.FinContract.IdFinAccount)];
            List<PlanTransaction> interestList = [.. clpt.Where(p => p.DebitAccount == CFM.Loan.IdAccountInterest && p.CreditAccount == CFM.FinContract.IdFinAccount)];
            List<PlanTransaction> paymentRateList = [.. clpt.Where(p => p.DebitAccount == CFM.FinContract.IdFinAccount && p.CreditAccount == OriginalContract.FinContract.IdPayAccount && p.IdSource == OriginalContract.FinContract.IdPayAccount)];
            List<PlanTransaction> coverRateList = [.. clpt.Where(p => p.DebitAccount == OriginalContract.FinContract.IdPayAccount && p.CreditAccount == OriginalContract.FinContract.IdBankAccount && p.IdSource == OriginalContract.FinContract.IdBankAccount)];
            // update of the PayAccount
            if (CFM.FinContract.IdPayAccount != OriginalContract.FinContract.IdPayAccount)
            {
                foreach (PlanTransaction pt in incomeRateList)
                {
                    pt.CreditAccount = CFM.FinContract.IdPayAccount;
                }
                foreach (PlanTransaction pt in paymentRateList)
                {
                    pt.CreditAccount = CFM.FinContract.IdPayAccount;
                    pt.IdSource = CFM.FinContract.IdPayAccount;
                }
            }
            int totalIter = Math.Min(Math.Min(incomeRateList.Count, paymentRateList.Count), interestList.Count);
            /*
             * Change rate only if the interest paid has changed, otherwise rate remains the same
             * Rate for fix is calculated based on the latest real balance - if user selected balance update or there are already recorded transactions (RestrictionOn == 1)
             */
            bool interestHasChanged = CFM.Loan.LoanInterest != OriginalContract.Loan.LoanInterest;
            CalculatorFinance calfin = new(CFM.Loan.LoanInterest * 0.01m, CFM.FinContract.IdFrequency);
            CalculatorDates caldate = new(CFM.FinContract.IdFrequency);
            DateTime startDateLoan = interestHasChanged ? caldate.LastCycleDate(CFM.FinContract.InvoiceDate, startDate) : CFM.FinContract.StartDate;
            int totalNumPayments = caldate.NumberCyclesPerPeriod(startDateLoan, CFM.FinContract.EndDate);
            calfin.TotalPayments = totalNumPayments;
            calfin.InitialBalance = interestHasChanged ? lbv : CFM.Loan.LoanAmount;
            calfin.ExtraPay = CFM.Loan.ExtraPay;
            bool changePayAccounts = (CFM.FinContract.IdPayAccount == OriginalContract.FinContract.IdPayAccount) || (CFM.FinContract.IdBankAccount == OriginalContract.FinContract.IdBankAccount);
            bool needCover = CFM.FinContract.IdPayAccount != CFM.FinContract.IdBankAccount;
            // remove all cover payments if change in payment accounts
            if (changePayAccounts && coverRateList.Count > 0)
            {
                _context.PlanTransactions.RemoveRange(coverRateList);
            }
            //
            // PlanTransactions are updated for the entire retrieved original list regardless if the balance goes negative
            // this is done to allow for further changes to be managed without need to change contract end date
            //
            if (CFM.Loan.IdLoanType)
            {
                // variable rate

                for (int i = 0; i < totalIter; i++)
                {
                    incomeRateList[i].TransactionValue = calfin.LoanVarRateOnBalance(lbv);
                    incomeRateList[i].CreditAccount = CFM.FinContract.IdPayAccount; // in case there is a change in the payment account
                    paymentRateList[i].TransactionValue = incomeRateList[i].TransactionValue;
                    paymentRateList[i].CreditAccount = CFM.FinContract.IdPayAccount; // in case there is a change in the payment account
                    paymentRateList[i].IdSource = CFM.FinContract.IdPayAccount;// in case there is a change in the payment account
                    _context.PlanTransactions.Attach(incomeRateList[i]).State = EntityState.Modified;
                    _context.PlanTransactions.Attach(paymentRateList[i]).State = EntityState.Modified;
                    interestList[i].TransactionValue = calfin.InterestValuePerCycle(lbv);
                    _context.PlanTransactions.Attach(interestList[i]).State = EntityState.Modified;
                    //
                    // cover payments
                    //
                    if (needCover)
                    {
                        PlanTransaction ptCover = new()
                        {
                            IdTransaction = GeneratorID.Int64IDBasedOnDate(incomeRateList[i].TransactionDate),
                            ContractIdentifier = incomeRateList[i].ContractIdentifier,
                            TransactionDate = incomeRateList[i].TransactionDate,
                            DebitAccount = CFM.FinContract.IdPayAccount,
                            CreditAccount = CFM.FinContract.IdBankAccount,
                            TransactionValue = incomeRateList[i].TransactionValue,
                            TransactionDetail = incomeRateList[i].TransactionDetail,
                            ExecutedTransaction = false,
                            IdSource = CFM.FinContract.IdBankAccount
                        };
                        _context.PlanTransactions.Add(ptCover);
                    }
                    lbv = lbv - incomeRateList[i].TransactionValue + interestList[i].TransactionValue; // liability balance is positive
                }
            }
            else
            {
                // fixed rate
                decimal newRate = interestHasChanged ? calfin.LoanFixedRate() : incomeRateList[0].TransactionValue; // change rate only if there was a change in interest
                for (int i = 0; i < totalIter; i++)
                {
                    if (lbv <= 0) { newRate = 0; } // cover when the balance is going negative
                    incomeRateList[i].TransactionValue = newRate;
                    incomeRateList[i].CreditAccount = CFM.FinContract.IdPayAccount; // in case there is a change in the payment account
                    paymentRateList[i].TransactionValue = incomeRateList[i].TransactionValue;
                    paymentRateList[i].CreditAccount = CFM.FinContract.IdPayAccount; // in case there is a change in the payment account
                    paymentRateList[i].IdSource = CFM.FinContract.IdPayAccount;// in case there is a change in the payment account
                    _context.PlanTransactions.Attach(incomeRateList[i]).State = EntityState.Modified;
                    _context.PlanTransactions.Attach(paymentRateList[i]).State = EntityState.Modified;
                    interestList[i].TransactionValue = calfin.InterestValuePerCycle(lbv);
                    _context.PlanTransactions.Attach(interestList[i]).State = EntityState.Modified;
                    //
                    // cover payments
                    //
                    if (needCover)
                    {
                        PlanTransaction ptCover = new()
                        {
                            IdTransaction = GeneratorID.Int64IDBasedOnDate(incomeRateList[i].TransactionDate),
                            ContractIdentifier = incomeRateList[i].ContractIdentifier,
                            TransactionDate = incomeRateList[i].TransactionDate,
                            DebitAccount = CFM.FinContract.IdPayAccount,
                            CreditAccount = CFM.FinContract.IdBankAccount,
                            TransactionValue = incomeRateList[i].TransactionValue,
                            TransactionDetail = incomeRateList[i].TransactionDetail,
                            ExecutedTransaction = false,
                            IdSource = CFM.FinContract.IdBankAccount
                        };
                        _context.PlanTransactions.Add(ptCover);
                    }
                    lbv = lbv - incomeRateList[i].TransactionValue + interestList[i].TransactionValue; // liability balance is positive
                }
            }
            // update EMR
            FrequencyConversion fc = new(CFM.FinContract.IdFrequency);
            EquivMonthRate = -1 * fc.CycletoMonthConversion(paymentRateList[0].TransactionValue);
            // add transaction to adjust the balance if a balance update for a non-executed loan
            if (RestrictionsOn == 0 && BalanceUpdateOK)
            {
                decimal delta = OriginalForeBalance - ForecastBalance;
                if (delta != 0)
                {
                    var uc = await _context.UserContracts.FindAsync(CFM.FinContract.IdContractor);
                    int receivable = uc != null ? uc.UserReceivable : 0;
                    int payable = uc != null ? uc.UserPayable : 0;
                    int debitAcc = 0;
                    int creditACC = 0;
                    if (delta < 0)
                    {
                        debitAcc = CFM.FinContract.IdFinAccount;
                        creditACC = payable;
                    }
                    else
                    {
                        debitAcc = receivable;
                        creditACC = CFM.FinContract.IdFinAccount;
                    }
                    PlanTransaction adj = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(startDate),
                        ContractIdentifier = CFM.FinContract.IdFinAccount,
                        TransactionDate = startDate,
                        DebitAccount = debitAcc,
                        CreditAccount = creditACC,
                        TransactionValue = Math.Abs(delta),
                        TransactionDetail = "Balance adjustment ",
                        ExecutedTransaction = false
                    };
                    _context.PlanTransactions.Add(adj);
                }
            }
            return string.Empty;
        }

        private async Task<string> UpdateCard(DateTime startDate)
        {
            // updates existing PlanTransaction records for the Deposit contract for a change in Rate or / and Interest
            // get list of plan transactions for the contract
            var clpt = await _context.PlanTransactions.AsQueryable().Where(c => c.ContractIdentifier == CFM.FinContract.IdFinAccount && c.TransactionDate >= startDate).OrderBy(d => d.TransactionDate).ToListAsync();
            if (clpt.Count == 0) return $"No plan transactions could be retrieved.";
            List<PlanTransaction> pTrans = [.. clpt];
            // set the plan and real balance right before startDate
            decimal purchBal = ForecastBalance;
            decimal cashBal = CardCashPlanBalance;

            if (RestrictionsOn == 1) { BalanceUpdateOK = true; }
            if (BalanceUpdateOK)
            {
                purchBal = RealBalance;
                cashBal = CardCashRealBalance;
            }
            // set the lists for incomeRate, payRate, interest, fee, insurance, and cover
            List<PlanTransaction> incomeRateList = [.. clpt.Where(p => p.DebitAccount == CFM.FinContract.IdFinAccount && p.CreditAccount == OriginalContract.FinContract.IdPayAccount && p.IdSource == CFM.FinContract.IdFinAccount)];
            List<PlanTransaction> cashRateList = [.. clpt.Where(p => p.DebitAccount == CFM.CreditCard.CashOutAccount && p.CreditAccount == OriginalContract.FinContract.IdPayAccount && p.IdSource == CFM.FinContract.IdFinAccount)];
            List<PlanTransaction> interestList = [.. clpt.Where(p => p.DebitAccount == CFM.CreditCard.InterestAccount && p.CreditAccount == CFM.FinContract.IdFinAccount)];
            List<PlanTransaction> interCOList = [.. clpt.Where(p => p.DebitAccount == CFM.CreditCard.InterestCashOutAcc && p.CreditAccount == CFM.FinContract.IdFinAccount)];
            List<PlanTransaction> feeList = [.. clpt.Where(p => p.DebitAccount == CFM.CreditCard.FeeAccount && p.CreditAccount == CFM.FinContract.IdFinAccount)];
            List<PlanTransaction> paymentRateList = [.. clpt.Where(p => p.DebitAccount == CFM.FinContract.IdFinAccount && p.CreditAccount == OriginalContract.FinContract.IdPayAccount && p.IdSource == OriginalContract.FinContract.IdPayAccount)];
            List<PlanTransaction> coverRateList = [.. clpt.Where(p => p.DebitAccount == OriginalContract.FinContract.IdPayAccount && p.CreditAccount == OriginalContract.FinContract.IdBankAccount && p.IdSource == OriginalContract.FinContract.IdBankAccount)];
            List<PlanTransaction> insuranceList = [.. clpt.Where(p => p.DebitAccount == CFM.Deposit.InterestAccount && p.CreditAccount == CFM.FinContract.IdFinAccount)];
            // update of the PayAccount
            if (CFM.FinContract.IdPayAccount != OriginalContract.FinContract.IdPayAccount)
            {
                foreach (PlanTransaction pt in incomeRateList)
                {
                    pt.CreditAccount = CFM.FinContract.IdPayAccount;
                }
                foreach (PlanTransaction pt in cashRateList)
                {
                    pt.CreditAccount = CFM.FinContract.IdPayAccount;
                }
                foreach (PlanTransaction pt in paymentRateList)
                {
                    pt.CreditAccount = CFM.FinContract.IdPayAccount;
                    pt.IdSource = CFM.FinContract.IdPayAccount;
                }
            }
            // calculate total number of elements to update as the minimum length out of all categories to guard against index overflow
            int temp_1 = Math.Min(incomeRateList.Count, paymentRateList.Count);
            int temp_2 = Math.Min(Math.Min(cashRateList.Count, interestList.Count), Math.Min(feeList.Count, insuranceList.Count));
            int totalPT = Math.Min(temp_1, temp_2);
            // remove all coverList elements
            if (coverRateList.Count > 0)
            {
                _context.PlanTransactions.RemoveRange(coverRateList);
            }
            // initialize the Finance calculator class
            CalculatorFinance calfin = new(CFM.CreditCard.CreditCardInterest * 0.01m, CFM.FinContract.IdFrequency)
            {
                ExtraPay = CFM.CreditCard.ExtraPay
            };
            bool dataChanged = CFM.CreditCard.CreditCardInterest != OriginalContract.CreditCard.CreditCardInterest;
            dataChanged = dataChanged || (CFM.CreditCard.CycleFee != OriginalContract.CreditCard.CycleFee);
            // update fees
            if (CFM.CreditCard.CycleFee != OriginalContract.CreditCard.CycleFee)
            {
                foreach (PlanTransaction fee in feeList)
                {
                    fee.TransactionValue = CFM.CreditCard.CycleFee;
                }
                _context.PlanTransactions.UpdateRange(feeList);
            }
            // update all rates
            decimal poffLimit = _config.GetSection("MySettings").GetValue<decimal>("CardPayOffLimit");
            if (poffLimit == 0) { poffLimit = 100; }
            decimal insurDec = CFM.CreditCard.CreditCardInsuranceRate * 0.01m; // value is listed in percent and transformed to decimal
            for (int i = 0; i < totalPT; i++)
            {
                incomeRateList[i].TransactionValue = calfin.CardRateOnBalance(purchBal, (decimal)CFM.CreditCard.CreditCardPayPercent, poffLimit);
                cashRateList[i].TransactionValue = calfin.CardRateOnBalance(cashBal, (decimal)CFM.CreditCard.CreditCardPayPercent, poffLimit, CFM.CreditCard.CashRate);
                interestList[i].TransactionValue = calfin.InterestValuePerCycle(purchBal);
                interCOList[i].TransactionValue = calfin.InterestValuePerCycle(cashBal, CFM.CreditCard.CashRate);
                insuranceList[i].TransactionValue = (purchBal + cashBal) * insurDec;
                paymentRateList[i].TransactionValue = incomeRateList[i].TransactionValue + cashRateList[i].TransactionValue + insuranceList[i].TransactionValue + feeList[i].TransactionValue;
                purchBal += interestList[i].TransactionValue - incomeRateList[i].TransactionValue;
                cashBal += interCOList[i].TransactionValue - cashRateList[i].TransactionValue;
            }
            // update EMR
            FrequencyConversion fc = new(CFM.FinContract.IdFrequency);
            EquivMonthRate = -1 * fc.CycletoMonthConversion(paymentRateList[0].TransactionValue);
            // attached the modified lists for recording
            _context.PlanTransactions.UpdateRange(incomeRateList);
            _context.PlanTransactions.UpdateRange(cashRateList);
            _context.PlanTransactions.UpdateRange(interestList);
            _context.PlanTransactions.UpdateRange(interCOList);
            _context.PlanTransactions.UpdateRange(insuranceList);
            _context.PlanTransactions.UpdateRange(paymentRateList);
            // record PlanTransaction for the variation between the Real Balance (user entered) and Plan Balance (as recorded)
            if (RestrictionsOn == 0 && BalanceUpdateOK)
            {
                decimal delta = OriginalForeBalance - ForecastBalance;
                if (delta != 0)
                {
                    var uc = await _context.UserContracts.FindAsync(CFM.FinContract.IdContractor);
                    int receivable = uc != null ? uc.UserReceivable : 0;
                    int payable = uc != null ? uc.UserPayable : 0;
                    int debitAcc = 0;
                    int creditACC = 0;
                    if (delta < 0)
                    {
                        debitAcc = CFM.FinContract.IdFinAccount;
                        creditACC = payable;
                    }
                    else
                    {
                        debitAcc = receivable;
                        creditACC = CFM.FinContract.IdFinAccount;
                    }
                    PlanTransaction adj = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(startDate),
                        ContractIdentifier = CFM.FinContract.IdFinAccount,
                        TransactionDate = startDate,
                        DebitAccount = debitAcc,
                        CreditAccount = creditACC,
                        TransactionValue = delta,
                        TransactionDetail = "Balance adjustment ",
                        ExecutedTransaction = false
                    };
                    _context.PlanTransactions.Add(adj);
                }
            }
            return string.Empty;
        }

        private async Task<string> UpdateEmployment(DateTime startDate, IConfiguration section)
        {
            string result = string.Empty;
            // updates existing PlanTransaction records for the Deposit contract for a change in Rate or / and Interest
            // get list of plan transactions for the contract
            var clpt = await _context.PlanTransactions.AsQueryable().Where(c => c.ContractIdentifier == CFM.FinContract.IdFinAccount && c.TransactionDate >= startDate).OrderBy(d => d.TransactionDate).ToListAsync();
            if (clpt.Count == 0) return $"No plan transactions identified";
            List<PlanTransaction> netIncomeList = [.. clpt.Where(p => p.CreditAccount == CFM.FinContract.IdFinAccount)];
            List<PlanTransaction> superList = [.. clpt.Where(p => p.DebitAccount == CFM.Employment.IdSuperAccount)];
            int idTaxAccount = await _context.UserContracts.Where(p => p.IdUser == CFM.FinContract.IdContractor).Select(s => s.UserTaxAccount).FirstOrDefaultAsync();
            List<PlanTransaction> taxList = [.. clpt.Where(p => p.DebitAccount == idTaxAccount)];
            // calculate total number of iterations
            int totalIteration = netIncomeList.Count;
            if (superList.Count > 0) { totalIteration = Math.Min(superList.Count, totalIteration); }
            if (taxList.Count > 0) { totalIteration = Math.Min(taxList.Count, totalIteration); }
            //update Pay Account if changed
            if (CFM.FinContract.IdPayAccount != OriginalContract.FinContract.IdPayAccount)
            {
                foreach (PlanTransaction net in netIncomeList)
                {
                    net.DebitAccount = CFM.FinContract.IdPayAccount;
                    net.IdSource = CFM.FinContract.IdPayAccount;
                }
            }
            // update Super account if changed
            if (CFM.Employment.IdSuperAccount != OriginalContract.Employment.IdSuperAccount)
            {
                foreach (PlanTransaction pt in superList)
                {
                    pt.DebitAccount = CFM.Employment.IdSuperAccount;
                }
            }
            // update rates
            if (CFM.Employment.GrossPaidCycle != OriginalContract.Employment.GrossPaidCycle)
            {
                foreach (PlanTransaction net in netIncomeList)
                {
                    net.TransactionValue = CFM.Employment.GrossPaidCycle - CFM.Employment.TaxPaidCycle;
                }
                _context.PlanTransactions.UpdateRange(netIncomeList);
            }
            // update tax
            if (CFM.Employment.TaxPaidCycle != OriginalContract.Employment.TaxPaidCycle)
            {
                if (taxList.Count > 0)
                {
                    // existing tax values
                    foreach (PlanTransaction tt in taxList)
                    {
                        tt.TransactionValue = CFM.Employment.TaxPaidCycle; // leave tax PTs even if value is zero for further updates
                    }
                    _context.PlanTransactions.UpdateRange(taxList);
                }
                else
                {
                    // no previous tax paid
                    string detailedTax = section.GetValue<string>("Tax") ?? "Tax paid from ";
                    for (int i = 0; i < netIncomeList.Count; i++)
                    {
                        PlanTransaction tax = new()
                        {
                            IdTransaction = GeneratorID.Int64IDBasedOnDate(netIncomeList[i].TransactionDate),
                            TransactionDate = netIncomeList[i].TransactionDate,
                            TransactionValue = CFM.Employment.TaxPaidCycle,
                            DebitAccount = idTaxAccount,
                            CreditAccount = CFM.FinContract.IdFinAccount,
                            TransactionDetail = detailedTax,
                            ExecutedTransaction = false,
                            ContractIdentifier = CFM.FinContract.IdFinAccount,
                            IdSource = CFM.FinContract.IdFinAccount
                        };
                        _context.PlanTransactions.Add(tax);
                    }
                }
            }
            // update super
            if (CFM.Employment.SuperPaidCycle != OriginalContract.Employment.SuperPaidCycle)
            {
                if (superList.Count > 0)
                {
                    //update value
                    if (CFM.Employment.SuperPaidCycle > 0)
                    {
                        //update value
                        foreach (PlanTransaction pt in superList)
                        {
                            pt.TransactionValue = CFM.Employment.SuperPaidCycle;
                        }
                        _context.PlanTransactions.UpdateRange(superList);
                    }
                    else
                    {
                        // delete existing
                        _context.PlanTransactions.RemoveRange(superList);
                    }

                }
                else
                {
                    string detailSuper = section.GetValue<string>("Super") ?? "Super payment from ";
                    for (int i = 0; i < netIncomeList.Count; i++)
                    {
                        PlanTransaction spr = new()
                        {
                            IdTransaction = GeneratorID.Int64IDBasedOnDate(netIncomeList[i].TransactionDate),
                            TransactionDate = netIncomeList[i].TransactionDate,
                            TransactionValue = CFM.Employment.SuperPaidCycle,
                            DebitAccount = CFM.Employment.IdSuperAccount,
                            CreditAccount = CFM.FinContract.IdFinAccount,
                            TransactionDetail = detailSuper,
                            ExecutedTransaction = false,
                            ContractIdentifier = CFM.FinContract.IdFinAccount,
                            IdSource = CFM.FinContract.IdFinAccount
                        };
                        _context.PlanTransactions.Add(spr);
                    }
                }
            }
            // update EMR
            FrequencyConversion fc = new(CFM.FinContract.IdFrequency);
            EquivMonthRate = fc.CycletoMonthConversion(CFM.Employment.GrossPaidCycle - CFM.Employment.TaxPaidCycle);
            return result;
        }

        private async Task<string> UpdateIncome(DateTime startDate, IConfiguration section)
        {
            string result = string.Empty;
            // updates existing PlanTransaction records for the Deposit contract for a change in Rate or / and Interest
            // get list of plan transactions for the contract
            var clpt = await _context.PlanTransactions.AsQueryable().Where(c => c.ContractIdentifier == CFM.FinContract.IdFinAccount && c.TransactionDate >= startDate).OrderBy(d => d.TransactionDate).ToListAsync();
            if (clpt.Count == 0) return $"No plan transactions identified";
            List<PlanTransaction> netIncomeList = [.. clpt.Where(p => p.CreditAccount == CFM.FinContract.IdFinAccount)];
            int idTaxAccount = await _context.UserContracts.Where(p => p.IdUser == CFM.FinContract.IdContractor).Select(s => s.UserTaxAccount).FirstOrDefaultAsync();
            List<PlanTransaction> taxList = [.. clpt.Where(p => p.DebitAccount == idTaxAccount)];
            // calculate total number of iterations
            int totalIteration = netIncomeList.Count;
            if (taxList.Count > 0) { totalIteration = Math.Min(taxList.Count, totalIteration); }
            //update Pay Account if changed
            if (CFM.FinContract.IdPayAccount != OriginalContract.FinContract.IdPayAccount)
            {
                foreach (PlanTransaction net in netIncomeList)
                {
                    net.DebitAccount = CFM.FinContract.IdPayAccount;
                    net.IdSource = CFM.FinContract.IdPayAccount;
                }
            }
            // update rates
            if (CFM.Income.GrossPerCycle != OriginalContract.Income.GrossPerCycle)
            {
                foreach (PlanTransaction net in netIncomeList)
                {
                    net.TransactionValue = CFM.Income.GrossPerCycle - CFM.Income.TaxPaidCycle;
                }
                _context.PlanTransactions.UpdateRange(netIncomeList);
            }
            // update tax
            if (CFM.Income.TaxPaidCycle != OriginalContract.Income.TaxPaidCycle)
            {
                if (taxList.Count > 0)
                {
                    // existing tax values
                    foreach (PlanTransaction tt in taxList)
                    {
                        tt.TransactionValue = CFM.Income.TaxPaidCycle; // leave tax PTs even if value is zero for further updates
                    }
                    _context.PlanTransactions.UpdateRange(taxList);
                }
                else
                {
                    // no previous tax paid
                    string detailedTax = section.GetValue<string>("Tax") ?? "Tax paid from ";
                    for (int i = 0; i < netIncomeList.Count; i++)
                    {
                        PlanTransaction tax = new()
                        {
                            IdTransaction = GeneratorID.Int64IDBasedOnDate(netIncomeList[i].TransactionDate),
                            TransactionDate = netIncomeList[i].TransactionDate,
                            TransactionValue = CFM.Income.TaxPaidCycle,
                            DebitAccount = idTaxAccount,
                            CreditAccount = CFM.FinContract.IdFinAccount,
                            TransactionDetail = detailedTax,
                            ExecutedTransaction = false,
                            ContractIdentifier = CFM.FinContract.IdFinAccount,
                            IdSource = CFM.FinContract.IdFinAccount
                        };
                        _context.PlanTransactions.Add(tax);
                    }
                }
            }
            // update EMR
            FrequencyConversion fc = new(CFM.FinContract.IdFrequency);
            EquivMonthRate = fc.CycletoMonthConversion(CFM.Income.GrossPerCycle - CFM.Income.TaxPaidCycle);
            return result;
        }

        private async Task<string> UpdateBill(DateTime startDate, IConfiguration section)
        {
            string result = string.Empty;
            // updates existing PlanTransaction records for the Deposit contract for a change in Rate or / and Interest
            // get list of plan transactions for the contract
            var clpt = await _context.PlanTransactions.AsQueryable().Where(c => c.ContractIdentifier == CFM.FinContract.IdFinAccount && c.TransactionDate >= startDate).OrderBy(d => d.TransactionDate).ToListAsync();
            if (clpt.Count == 0) return $"No plan transactions identified for the bill {CFM.FinContract.ContractName}";
            string detailBill = section.GetValue<string>("Rate") ?? "Payment to ";
            List<PlanTransaction> ratesList = [.. clpt.Where(p => p.DebitAccount == CFM.FinContract.IdFinAccount)];
            List<PlanTransaction> coverList_P = [.. clpt.Where(p => p.DebitAccount == OriginalContract.FinContract.IdPayAccount && p.CreditAccount == OriginalContract.FinContract.IdBankAccount && p.IdSource == OriginalContract.FinContract.IdPayAccount)]; // here are both sources idpayaccount and idbankaccount
            List<PlanTransaction> coverList_B = [.. clpt.Where(p => p.DebitAccount == OriginalContract.FinContract.IdPayAccount && p.CreditAccount == OriginalContract.FinContract.IdBankAccount && p.IdSource == OriginalContract.FinContract.IdBankAccount)]; // here are both sources idpayaccount and idbankaccount
            bool bhc_P = coverList_P.Count > 0;
            bool bhc_B = coverList_B.Count > 0;
            // remove cover if there is a variation in the number of elements between the two lists
            if (coverList_P.Count != coverList_B.Count)
            {
                coverList_P.Clear();
                coverList_B.Clear();
                if (bhc_P) { _context.PlanTransactions.RemoveRange(coverList_P); }
                if (bhc_B) { _context.PlanTransactions.RemoveRange(coverList_B); }
                bhc_P = false;
                bhc_B = false;
            }
            //update payment account if changed
            if (CFM.FinContract.IdPayAccount != OriginalContract.FinContract.IdPayAccount)
            {
                foreach (PlanTransaction pt in ratesList)
                {
                    pt.CreditAccount = CFM.FinContract.IdPayAccount;
                    pt.IdSource = CFM.FinContract.IdPayAccount;
                }
                if (bhc_P)
                {
                    foreach (PlanTransaction pt in coverList_P)
                    {
                        pt.DebitAccount = CFM.FinContract.IdPayAccount;
                        pt.IdSource = CFM.FinContract.IdPayAccount;
                    }
                }
                if (bhc_B)
                {
                    foreach (PlanTransaction pt in coverList_B)
                    {
                        pt.DebitAccount = CFM.FinContract.IdPayAccount;
                    }
                }
            }
            // update bank account if changed
            if (CFM.FinContract.IdBankAccount != OriginalContract.FinContract.IdBankAccount)
            {
                if (bhc_P && bhc_B)
                {
                    foreach (PlanTransaction pt in coverList_P)
                    {
                        pt.CreditAccount = CFM.FinContract.IdBankAccount;
                    }
                    foreach (PlanTransaction pt in coverList_B)
                    {
                        pt.CreditAccount = CFM.FinContract.IdBankAccount;
                        pt.IdSource = CFM.FinContract.IdBankAccount;
                    }
                }

            }
            // update rate if changed
            if (CFM.Bill.BillCycleRate != OriginalContract.Bill.BillCycleRate)
            {
                foreach (PlanTransaction rate in ratesList)
                {
                    rate.TransactionValue = CFM.Bill.BillCycleRate;
                }
                _context.PlanTransactions.UpdateRange(ratesList);
            }
            // update payment account cover transactions if required
            bool payFromLiability = CFM.FinContract.IdPayAccount.ToString().StartsWith('2');
            if ((CFM.FinContract.IdPayAccount != CFM.FinContract.IdBankAccount) || payFromLiability)
            {
                // bill cover payments are required
                string detailReceive = section.GetValue<string>("Income") ?? "Payment from ";
                string detailPay = section.GetValue<string>("Rate") ?? "Payment to ";
                if (bhc_P && bhc_B)
                {
                    // update existing cover plan transactions
                    foreach (PlanTransaction pt in coverList_P)
                    {
                        pt.TransactionValue = CFM.Bill.BillCycleRate;
                    }
                    foreach (PlanTransaction rate in coverList_B)
                    {
                        rate.TransactionValue = CFM.Bill.BillCycleRate;
                    }
                    _context.PlanTransactions.UpdateRange(coverList_P);
                    _context.PlanTransactions.UpdateRange(coverList_B);
                }
                else
                {
                    // create new cover plan transactions
                    for (int i = 0; i < ratesList.Count; i++)
                    {
                        PlanTransaction pay = new()
                        {
                            IdTransaction = GeneratorID.Int64IDBasedOnDate(ratesList[i].TransactionDate),
                            TransactionDate = ratesList[i].TransactionDate,
                            TransactionValue = ratesList[i].TransactionValue,
                            DebitAccount = CFM.FinContract.IdPayAccount,
                            CreditAccount = CFM.FinContract.IdBankAccount,
                            TransactionDetail = detailReceive,
                            ExecutedTransaction = false,
                            IdSource = CFM.FinContract.IdPayAccount
                        };
                        _context.PlanTransactions.Add(pay);
                        PlanTransaction bank = new()
                        {
                            IdTransaction = GeneratorID.Int64IDBasedOnDate(ratesList[i].TransactionDate),
                            TransactionDate = ratesList[i].TransactionDate,
                            TransactionValue = ratesList[i].TransactionValue,
                            DebitAccount = CFM.FinContract.IdPayAccount,
                            CreditAccount = CFM.FinContract.IdBankAccount,
                            TransactionDetail = detailPay,
                            ExecutedTransaction = false,
                            IdSource = CFM.FinContract.IdBankAccount
                        };
                        _context.PlanTransactions.Add(bank);
                    }
                }
            }
            // update EMR
            FrequencyConversion fc = new(CFM.FinContract.IdFrequency);
            EquivMonthRate = -1 * fc.CycletoMonthConversion(ratesList[0].TransactionValue);
            return result;
        }

        private async Task<string> UpdateContractPlanTransactions(DateTime fromDate)
        {
            IConfiguration _section = _config.GetSection("TransactionDetail");
            string result = string.Empty;
            if (OriginalContract is null) { return $"Ooops an error was enountered retrieving the original data for contract {CFM.FinContract.ContractName}"; }
            switch (CFM.FinContract.ContractType)
            {
                case 1:
                    result = await UpdateDeposit(fromDate);
                    break;
                case 2:
                    result = await UpdateLoan(fromDate);
                    break;
                case 3:
                    result = await UpdateCard(fromDate);
                    break;
                case 4:
                    result = await UpdateEmployment(fromDate, _section);
                    break;
                case 5:
                    result = await UpdateIncome(fromDate, _section);
                    break;
                case 6:
                    result = await UpdateBill(fromDate, _section);
                    break;
                case 7:
                    result = await UpdateBill(fromDate, _section);
                    break;
            }
            if (result != string.Empty) { return result; }
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result = ex.Message + "\n" + ex.InnerException + "\n" + "Error in updating the contract's plan transactions";
            }
            return result;
        }

        private async Task<string> ReplacePlanTransactions(DateTime startDate)
        {
            // Delete all plan transactions from startDate
            TransactionHelper thel = new(_context, CFM, _config);
            string result = await thel.DeletePlanTransactions(startDate);
            if (result.Length > 0) { return result; }
            if (RestrictionsOn == 1)
            {
                BalanceUpdateOK = true;
            }
            decimal purchBalance = ForecastBalance;
            decimal cashBalance = CardCashPlanBalance;
            if (BalanceUpdateOK)
            {
                purchBalance = RealBalance;
                cashBalance = CardCashRealBalance;
            }
            await thel.AddPlanTransactions(startDate, purchBalance, cashBalance);
            EquivMonthRate = thel.EMR;
            return result;
        }
        #endregion updateprocedures

    }
}
