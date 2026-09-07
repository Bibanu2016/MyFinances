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
using System.Xml.Linq;

namespace MyFinances.Pages.Contracts
{
    public class ContractAddModel(AppDBContext context, IConfiguration configuration, IMemoryCache memoryCache, IComboFillService comboFill) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly IComboFillService _comboFill = comboFill;
        private readonly string[] payableRoles = ["PaymentAsset", "PaymentLiability"];
        private readonly string[] bankRoles = ["PaymentAsset"];
        private readonly string[] superRoles = ["InvestmentFund"];
        //
        // contract type selection variable
        [BindProperty(SupportsGet =true)]
        public short SelectedContractType { get; set; } = 0;


        // Combobox source data
        public SelectList Frequency { get; set; } = default!;
        public SelectList EndAction { get; set; } = default!;
        public SelectList Organisation { get; set; } = default!;
        public SelectList Payable { get; set; } = default!;
        public SelectList Bank { get; set; } = default!;
        public SelectList Users { get; set; } = default!;
        public SelectList TangibleAsset { get; set; } = default!;
        public SelectList ContractCategory { get; set; } = default!;
        public SelectList SuperAccount { get; set; } = default!;

        // Contract data
        [BindProperty]
        public ContractFull ContractFull { get; set; } = default!;

        [BindProperty, Display(Name ="Contract category"), Range(1000, 9999, ErrorMessage ="Value {0} shall be between {1} and {2}")]
        public int CategoryContract { get; set; } = 0;
        [BindProperty, DataType(DataType.Currency), Range(0, 1000000000, ErrorMessage ="Value {0} is required in the range {1} to {2}"), Display(Name ="Current balance")]
        public decimal PurchaseBalance { get; set; } = 0;
        [BindProperty, DataType(DataType.Currency), Range(0, 1000000000, ErrorMessage = "Value {0} is required in the range {1} to {2}"), Display(Name = "Cash balance card")]
        public decimal CashBalance { get; set; } = 0;
        [BindProperty, DataType(DataType.DateTime), Display(Name = "Date of current balance")]
        public DateTime BalanceDate { get; set; } = DateTime.Now;

        // GET Method
        public async Task<PageResult> OnGetAsync(int? sct)
        {
            if (_memoryCache.TryGetValue("UserFCont", out FinContract? UserFCont))
            {
                if (UserFCont != null)
                {
                    ContractFull.FinContract = UserFCont;
                    _memoryCache.Remove("UserFCont");
                }
            }
            else
            {
                if (sct.HasValue)
                {
                    SelectedContractType = (short)sct.Value;
                    var ct = await _context.ContractTypes.FindAsync((short)sct);
                    if (ct != null)
                    {
                        ViewData["Title"] = $"Create {ct.ContractTypeName} contract";
                    }

                }
                else
                {
                    ViewData["Title"] = "No contract type selected!";
                }
            }
            await FillUpLists();            
            return Page();
        }
        //
        // POST Method
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // --- model validation start ---
            TailorBindingProperties();
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
            TransactionHelper trh = new(_context, ContractFull, _configuration);
            if (SelectedContractType == 3)
            {
                decimal cpol = _configuration.GetSection("MySettings").GetValue<decimal>("CardPayOffLimit");
                if (cpol == 0) cpol = 100m;
                await trh.AddPlanTransactions(DateTime.Today, PurchaseBalance, CashBalance, cpol);
            }
            else
            {
                await trh.AddPlanTransactions(DateTime.Today, PurchaseBalance);
            }
            ContractFull.FinContract.EquivMonthlyRate = trh.EMR;
            // record updates to the database
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int x = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Contract {ContractFull.FinContract.ContractName} has been successfuly created! \n A total of {x} records inserted." +
                    $"\n Contract account number is {ContractFull.FinContract.IdFinAccount}";
            }
            catch (DbException ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error during saving to the database. \n {ex.Message}";
            }
            return RedirectToPage("./ContractsList");
        }

        // --- Procedures to create the lists ---
        /// <summary>
        /// Populates the lists with the data from the database
        /// </summary>
        /// <returns></returns>
        private async Task FillUpLists()
        {
            if (SelectedContractType < 1 || SelectedContractType > 7) {return; }
            Frequency = await _memoryCache.GetOrCreateAsync("FRE", () => _comboFill.GetPaymentFrequencyList());
            EndAction = await _memoryCache.GetOrCreateAsync("ENDA", () => _comboFill.GetContractEndActionList());
            Organisation = await _memoryCache.GetOrCreateAsync("ORGS", () => _comboFill.GetOrganisationsList());
            Payable = await _memoryCache.GetOrCreateAsync("PAYS", () => _comboFill.GetFinAccountListByRole(payableRoles));
            Bank = await _memoryCache.GetOrCreateAsync("BANKS", () => _comboFill.GetFinAccountListByRole(bankRoles));
            Users = await _memoryCache.GetOrCreateAsync("USRS", () => _comboFill.GetUsersList());
            ContractCategory = await _memoryCache.GetOrCreateAsync("CTGY", () => _comboFill.GetAccountCategoryList(SelectedContractType));
            if(SelectedContractType == 4 || SelectedContractType == 5) 
            {
                SuperAccount = await _memoryCache.GetOrCreateAsync("SUPA", () => _comboFill.GetFinAccountListByRole(superRoles));
            }
            SuperAccount = await _memoryCache.GetOrCreateAsync("SUPA", () => _comboFill.GetFinAccountListByRole(superRoles));
            TangibleAsset = await _memoryCache.GetOrCreateAsync("TANG", () => _comboFill.GetTangibleAssetsList());
            return;
        }

        private void TailorBindingProperties()
        {
            //remove unwanted fields - use xml file
            var csf = XDocument.Load("./Data/ContractSuplFields.xml").Descendants("supfield").Select(x => new{ctg = x.Attribute("categ"), val = x.Value });
            if (csf != null)
            {
                foreach (var c in csf)
                {
                    if(c.ctg != null && c.val != null)
                    {
                        if((short)(int)c.ctg != SelectedContractType) { this.ModelState.Remove("ContractFull." + (string)c.val); }
                    }
                }
            }
            if(SelectedContractType > 3)
            {
                this.ModelState.Remove("BalanceDate");
                this.ModelState.Remove("PurchaseBalance");
                this.ModelState.Remove("CashBalance");
            }
            return;
        }

        private void CustomValidatorContract()
        {
            if (SelectedContractType > 0 && SelectedContractType < 4)
            {
                // guard against a balance date later than today
                if (BalanceDate > DateTime.Today) { this.ModelState.TryAddModelError("BalanceDate", "A date earlier than or today is required"); }
                // guard against a balance date ealier than 
                if (BalanceDate < ContractFull.FinContract.StartDate) { this.ModelState.TryAddModelError("BalanceDate", "A date later than or the contact Start Date is required"); }
                CalculatorDates calculatorDates = new(ContractFull.FinContract.IdFrequency);
                DateTime lastPayment = calculatorDates.LastCycleDate(ContractFull.FinContract.InvoiceDate, DateTime.Now);
                if(BalanceDate < lastPayment) { this.ModelState.TryAddModelError("BalanceDate", $"A date later than last peyment date of {lastPayment} is required."); }
                DateTime currentFY = new GeneralHelper(_configuration).StartFinancialYear(DateTime.Now);
                if(BalanceDate < currentFY) { this.ModelState.TryAddModelError("BalanceDate", $"A date later than the start of the current financial year of {currentFY} is required."); }
            }
            if (SelectedContractType == 2)
            {
                // balance at date less than the initial amount
                if(PurchaseBalance > ContractFull.Loan.LoanAmount) { this.ModelState.TryAddModelError("PurchaseBalance", "A value less or equal to Initial Loan Amount is required"); }
            }
            if (SelectedContractType == 3)
            {
                // purchase balance less than the credit card limit
                if (PurchaseBalance > ContractFull.CreditCard.CreditCardLimit) { this.ModelState.TryAddModelError("PurchaseBalance", "A value less or equal to Card Limit is required"); }
                //cash balance less than credit limit minus purchase balance
                if(CashBalance > Math.Abs(ContractFull.CreditCard.CreditCardLimit - PurchaseBalance)) { this.ModelState.TryAddModelError("CashBalance", "A value less or equal to Card Limit minus Purchase Balance is required"); }
            }
            return;
        }

        private async Task RecordContractData()
        {
            //get contract idfinaccount
            FinAccountHelper fah = new(_context, _configuration);
            //update contract data
            ContractFull.FinContract.IdFinContract = Guid.NewGuid();
            bool iscc = SelectedContractType == 3;
            int[] tempids = await fah.NewIdAccountByCateg(CategoryContract, iscc);
            ContractFull.FinContract.IdFinAccount = tempids[0];
            ContractFull.FinContract.ContractType = SelectedContractType;
            ContractFull.FinContract.IsActiveContract = true;
            // create and add contract account
            FinAccount cfa = new()
            {
                IdFinAccount = ContractFull.FinContract.IdFinAccount,
                Name = ContractFull.FinContract.ContractName,
                AccountCategory = (short)CategoryContract,
                AccountOwner = ContractFull.FinContract.IdContractor,
                IsLinked = true
            };
            _context.FinAccounts.Add(cfa);
            // add contract supplement
            IConfiguration sectionDetail = _configuration.GetSection("TransactionDetail");
            IConfiguration sectionCategory = _configuration.GetSection("AccountCategoriesSettings");
            switch (SelectedContractType)
            {
                case 1:
                    ContractFull.Deposit.IdDeposit = ContractFull.FinContract.IdFinContract;
                    short interestCtg = sectionCategory.GetValue<short>("InterestReceived");
                    interestCtg = (short)(interestCtg == 0? 4102 : interestCtg); // in case that there is an error reading the value, the category for Interest Received = 4102
                    ContractFull.Deposit.InterestAccount = (await fah.NewIdAccountByCateg(interestCtg))[0];
                    FinAccount DepInterest = new()
                    {
                        IdFinAccount = ContractFull.Deposit.InterestAccount,
                        Name = string.Concat(sectionDetail.GetValue<string>("InterestIn") ??"Interest received ", ContractFull.FinContract.ContractName),
                        AccountCategory = interestCtg,
                        AccountOwner = ContractFull.FinContract.IdContractor,
                        IsLinked = true
                    };
                    Balance dbal = new()
                    {
                        IdBalance = GeneratorID.Int64IDBasedOnDate(BalanceDate),
                        IdFinAccount = ContractFull.FinContract.IdFinAccount,
                        BalanceDate = BalanceDate,
                        BalanceValue = PurchaseBalance,
                        CashValue = 0
                    };
                    _context.Balances.Add(dbal);
                    _context.FinAccounts.Add(DepInterest);
                    _context.Deposits.Add(ContractFull.Deposit);
                    break;
                case 2:
                    ContractFull.Loan.IdLoan = ContractFull.FinContract.IdFinContract;
                    short lia = sectionCategory.GetValue<short>("InterestPaid");
                    lia = (short)(lia == 0? 5004 : lia); // in case that there is an error reading the value, the category for Interest Paid = 5004
                    ContractFull.Loan.IdAccountInterest = (await fah.NewIdAccountByCateg(lia))[0];
                    FinAccount LoanInterest = new()
                    {
                        IdFinAccount = ContractFull.Loan.IdAccountInterest,
                        Name = string.Concat(sectionDetail.GetValue<string>("InterestOut") ?? "Interest charge ", ContractFull.FinContract.ContractName),
                        AccountCategory = lia,
                        AccountOwner = ContractFull.FinContract.IdContractor,
                        IsLinked = true
                    };
                    Balance lbal = new()
                    {
                        IdBalance = GeneratorID.Int64IDBasedOnDate(BalanceDate),
                        IdFinAccount = ContractFull.FinContract.IdFinAccount,
                        BalanceDate = BalanceDate,
                        BalanceValue = -1.00m * PurchaseBalance,
                        CashValue = 0
                    };
                    _context.Balances.Add(lbal);
                    _context.FinAccounts.Add(LoanInterest);
                    _context.Loans.Add(ContractFull.Loan);
                    break;
                case 3:
                    ContractFull.CreditCard.IdCreditCard = ContractFull.FinContract.IdFinContract;
                    var cia = sectionCategory.GetValue<short>("InterestPaid");
                    cia = (short)(cia == 0 ? 5004 : cia); // in case that there is an error reading the value, the category for Interest Paid = 5004
                    int[] interIDs = await fah.NewIdAccountByCateg((short)cia, true);
                    ContractFull.CreditCard.InterestAccount = interIDs[0];
                    string detInterest = sectionDetail.GetValue<string>("InterestOut") ?? "Interest charge ";
                    FinAccount CardInterest = new() //purchase interest account
                    {
                        IdFinAccount = ContractFull.CreditCard.InterestAccount,
                        Name = string.Concat(detInterest, ContractFull.FinContract.ContractName),
                        AccountCategory = cia,
                        AccountOwner = ContractFull.FinContract.IdContractor,
                        IsLinked = true
                    };
                    _context.FinAccounts.Add(CardInterest);
                    ContractFull.CreditCard.InterestCashOutAcc = interIDs[1];
                    FinAccount COInterest = new() // cash out interest account
                    {
                        IdFinAccount = ContractFull.CreditCard.InterestCashOutAcc,
                        Name = string.Concat("Interest charge cash out ", ContractFull.FinContract.ContractName),
                        AccountCategory = cia,
                        AccountOwner = ContractFull.FinContract.IdContractor,
                        IsLinked = true
                    };
                    _context.FinAccounts.Add(COInterest);
                    if (ContractFull.CreditCard.CreditCardInsuranceRate > 0)
                    {
                        short cca = sectionCategory.GetValue<short>("InsuranceCredit");
                        cca = (short)(cca == 0 ? 5015 : cca);
                        ContractFull.CreditCard.InsuranceAccount = (await fah.NewIdAccountByCateg((short)cca))[0];
                        FinAccount CardInsurance = new()
                        {
                            IdFinAccount = ContractFull.CreditCard.InsuranceAccount,
                            Name = string.Concat(sectionCategory.GetValue<string>("Insurance") ?? "Insurance charge ", ContractFull.FinContract.ContractName),
                            AccountCategory = cca,
                            AccountOwner = ContractFull.FinContract.IdContractor,
                            IsLinked = true
                        };
                        _context.FinAccounts.Add(CardInsurance);
                    }
                    short fac = sectionCategory.GetValue<short>("Fees");
                    fac = (short)(fac == 0 ? 5010 : fac);
                    ContractFull.CreditCard.FeeAccount = (await fah.NewIdAccountByCateg((short)fac))[0];
                    FinAccount CardFee = new()
                    {
                        IdFinAccount = ContractFull.CreditCard.FeeAccount,
                        Name = string.Concat(sectionDetail.GetValue<string>("Fee") ?? "Fee ", ContractFull.FinContract.ContractName),
                        AccountCategory = (short)fac,
                        AccountOwner = ContractFull.FinContract.IdContractor,
                        IsLinked = true
                    };
                    _context.FinAccounts.Add(CardFee);
                    FinAccount CardCashOut = new()
                    {
                        IdFinAccount = tempids[1],
                        Name = "Cash out " + ContractFull.FinContract.ContractName,
                        AccountCategory = (short)CategoryContract,
                        AccountOwner = ContractFull.FinContract.IdContractor,
                        IsLinked = true
                    };
                    Balance cbal = new()
                    {
                        IdBalance = GeneratorID.Int64IDBasedOnDate(BalanceDate),
                        IdFinAccount = ContractFull.FinContract.IdFinAccount,
                        BalanceDate = BalanceDate,
                        BalanceValue = -1.00m * PurchaseBalance,
                        CashValue = -1.00m * CashBalance
                    };
                    _context.Balances.Add(cbal);
                    _context.FinAccounts.Add(CardCashOut);
                    _context.CreditCards.Add(ContractFull.CreditCard);
                    break;
                case 4:
                    ContractFull.Employment.IdEmployment = ContractFull.FinContract.IdFinContract;
                    _context.Employments.Add(ContractFull.Employment);
                    break;
                case 5:
                    ContractFull.Income.IdIncome = ContractFull.FinContract.IdFinContract;
                    _context.Incomes.Add(ContractFull.Income);
                    break;
                case 6:
                    ContractFull.Bill.IdBill = ContractFull.FinContract.IdFinContract;
                    _context.Bills.Add(ContractFull.Bill);
                    break;
                case 7:
                    ContractFull.Bill.IdBill = ContractFull.FinContract.IdFinContract;
                    _context.Bills.Add(ContractFull.Bill);
                    break;
                default:
                    break;
            }
            _context.FinContracts.Add(ContractFull.FinContract);
            return;
        }

        private async Task<string> RecordInitialTransaction(DateTime transacDate)
        {
            // recording is done only if the InitialAmount differs from the Purchase balance + Cash balance
            // and balance date is within current financial year
            // get user account id
            if(SelectedContractType > 3) { return string.Empty; }
            var uaid = await _context.UserContracts.Where(p=>p.IdUser == ContractFull.FinContract.IdContractor).Select(s=>s.UserAccountID).FirstOrDefaultAsync();
            string detailOpen = _configuration.GetSection("TransactionDetail").GetValue<string>("Open") ?? "Open account ";
            PlanTransaction openPt = new()
            {
                case 1:
                    PlanTransaction fin = new()
                    {
                IdTransaction = GeneratorID.Int64IDBasedOnDate(transacDate),
                TransactionDate = transacDate,
                        DebitAccount = ContractFull.FinContract.IdFinAccount,
                        CreditAccount = uaid,
                TransactionValue = PurchaseBalance,
                TransactionDetail = detailOpen,
                ContractIdentifier = ContractFull.FinContract.IdFinAccount,
                ExecutedTransaction = false,
                IdSource = uaid
            };
            Balance oab = new()
            {
                IdBalance = GeneratorID.Int64IDBasedOnDate(transacDate),
                IdFinAccount = ContractFull.FinContract.IdFinAccount,
                BalanceDate = transacDate,
            };
            switch(SelectedContractType)
            {
                case 1:
                    openPt.DebitAccount = ContractFull.FinContract.IdFinAccount;
                    openPt.CreditAccount = uaid;
                    oab.BalanceValue = PurchaseBalance;
                    oab.CashValue = 0;
                    _context.Balances.Add(oab);
                    break;
                case 2:
                    openPt.DebitAccount = uaid;
                    openPt.CreditAccount = ContractFull.FinContract.IdFinAccount;
                    oab.BalanceValue = PurchaseBalance * -1.0m;
                    oab.CashValue = 0;
                    _context.Balances.Add(oab);
                    break;
                case 3:
                    openPt.DebitAccount = uaid;
                    openPt.CreditAccount = ContractFull.FinContract.IdFinAccount;
                    PlanTransaction cardCashOpen = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(transacDate),
                        TransactionDate = transacDate,
                        DebitAccount = uaid,
                        CreditAccount = ContractFull.CreditCard.CashOutAccount,
                        TransactionValue = CashBalance,
                        TransactionDetail = detailOpen,
                        ContractIdentifier = ContractFull.FinContract.IdFinAccount,
                        ExecutedTransaction = false,
                        IdSource = uaid
                    };
                    _context.PlanTransactions.Add(cardOpen);
                    _context.PlanTransactions.Add(cardCashOpen);
                    oab.BalanceValue = -1.0m * ( PurchaseBalance + CashBalance );
                    oab.CashValue = -1.0m * CashBalance;
                    _context.Balances.Add(oab);
                    break;
                case 8:
                    PlanTransaction superOpen = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(transacDate),
                        TransactionDate = transacDate,
                        DebitAccount = ContractFull.FinContract.IdFinAccount,
                        CreditAccount = uaid,
                        TransactionValue = PurchaseBalance,
                        TransactionDetail = detailOpen,
                        ContractIdentifier = ContractFull.FinContract.IdFinAccount,
                        ExecutedTransaction = false,
                        IdSource = uaid
                    };
                    _context.PlanTransactions.Add(superOpen);
                    break;
                default:
                    break;
            }
            _context.PlanTransactions.Add(openPt);
            return string.Empty ;
        }

    }
}
