using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceCalculator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using MyFinances.ViewModels;

namespace MyFinances.Pages.Contracts
{
    public class ContractDetailsModel(AppDBContext context, IMyDetailsService detailsService, IConfiguration configuration) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IMyDetailsService _myDetailsService = detailsService;
        private readonly IConfiguration _configuration = configuration;

        public ContractFull Full { get; private set; } = default!;

        public FinContractNames FullNames { get; private set; } = new();

        public bool HasTransactions { get; private set; } = false;

        public string MissingMonths { get; private set; } = string.Empty;

        public decimal LastBalance { get; private set; } = 0;
        public decimal LastCashOutBal { get; private set; } = 0;
        public DateTime BalanceDate { get; private set;  } = DateTime.Now;
        public decimal NextRateValue { get; private set; } = 0;
        public DateTime NextRateDate { get; private set; } = DateTime.Now;
        public decimal FYStartBalance { get; private set; } = 0;
        public decimal[] CardRates123 { get; private set; } = [0,0,0];

        internal List<string> MonthsName { get; private set; } = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            FinContractHelper fch = new(_context, _configuration);
            Full = await fch.GetContractOnID(id?? Guid.Empty);
            if(Full == default) { return NotFound(); }
            // get the names for the ids in the contract
            ViewData["Title"] = $"Details contract {Full.FinContract.ContractName}";
            FullNames.ContractTypeName = await _myDetailsService.GetContractTypeByID(Full.FinContract.ContractType);
            FullNames.OrganisationName = await _myDetailsService.GetOrganisationNameByID(Full.FinContract.IdOrganisation);
            FullNames.ContractortName = await _myDetailsService.GetUserNameByID(Full.FinContract.IdContractor);
            FullNames.EndActionName = await _myDetailsService.GetEndActionNameByID(Full.FinContract.IdEndAction);
            FullNames.FrequencyName = await _myDetailsService.GetFrequencyNameByID(Full.FinContract.IdFrequency);
            FullNames.PaymentAccountName = await _myDetailsService.GetAccountNameByID(Full.FinContract.IdPayAccount);
            FullNames.BankAccountName = await _myDetailsService.GetAccountNameByID(Full.FinContract.IdBankAccount);
            if(Full.FinContract.ContractType == 2)
            {
                FullNames.LoanTypeName = Full.Loan.IdLoanType ? "VARIABLE" : "FIX";
                FullNames.LoanSecurityName = await _myDetailsService.GetAssetNameByID(Full.Loan.LoanSecurity ?? Guid.Empty);
            }
            if(Full.FinContract.ContractType == 4)
            {
                FullNames.SuperAccountName = await _myDetailsService.GetAccountNameByID(Full.Employment.IdSuperAccount);
            }
            HasTransactions = await _context.FinTransactions.Where(p=>p.ContractIdentifier == Full.FinContract.IdFinAccount).FirstOrDefaultAsync() != null;
            List<int> mmList = await fch.GetNoRecTransacMonths(Full.FinContract, DateTime.Now);
            if (mmList.Count > 0)
            {
                int y;
                int m;
                StringBuilder sb = new();
                foreach (var mm in mmList)
                {
                    y = mm / 100;
                    m = mm % 100;
                    sb.Append(MonthsName[m - 1]);
                    sb.Append('-');
                    sb.Append(y);
                    sb.Append(';');
                    sb.Append(' ');
                }
                MissingMonths = sb.ToString();
            }
            // if Deposit, Loan, or Credit Card
            // Provide --------------
            // Next invoice date ***
            // Next invoice rate ***
            // Current real balance ***
            // Balance date ***
            // Current plan balance
            if(Full.FinContract.ContractType < 4) // Deposit, Loan, or Credit Card
            {
                LastBalance = await fch.GetContractRealBalance(Full.FinContract.IdFinAccount, DateTime.Now);
                if(Full.FinContract.ContractType == 3) 
                { 
                    LastCashOutBal = await fch.GetContractRealBalance(Full.FinContract.IdFinAccount, DateTime.Now, Full.CreditCard.CashOutAccount);
                    List<decimal> colist = CardPayOffOptions(LastBalance, Full.CreditCard.CreditCardInterest * 0.01m, Full.FinContract.IdFrequency);
                    for(int i = 0; i<3;i++)
                    {
                        CardRates123[i] = colist[i];
                    }
                }
                BalanceDate = await LastRecordedTransaction(DateTime.Now, Full.FinContract);
                FYStartBalance = await BalanceAtStartFY(Full.FinContract);
            }
            PlanTransaction npt = await NextPlanTransaction(DateTime.Now, Full.FinContract);
            if(npt != null)
            {
                NextRateDate = npt.TransactionDate;
                NextRateValue = npt.TransactionValue;
            }
            return Page();
        }

        private async Task<PlanTransaction> NextPlanTransaction(DateTime refDate, FinContract fc)
        {
            PlanTransaction result = default!;
            CalculatorDates calcDate = new(fc.IdFrequency);
            var npt = await _context.PlanTransactions.Where(p=>p.ContractIdentifier == fc.IdFinAccount && p.TransactionDate >= refDate).AsNoTracking().FirstOrDefaultAsync();
            if (npt != null) { result = npt; }
            return result;
        }

        private async Task<DateTime> LastRecordedTransaction(DateTime refDate, FinContract fc)
        {
            var temp = await _context.FinTransactions.Where(p => p.ContractIdentifier == fc.IdFinAccount && p.TransactionDate <= refDate).Select(s=>s.TransactionDate).ToListAsync();
            if (temp.Count > 0)
            {
                return temp.Max();
            }
            else
            {
                return DateTime.Parse("2001-01-01");
            }
        }

        private async Task<decimal> BalanceAtStartFY(FinContract fc)
        {
            decimal result = 0;
            if(fc.ContractType < 4)
            {
                var uca = await _context.UserContracts.Where(p=>p.IdUser == fc.IdContractor).Select(s=>s.UserAccountID).FirstOrDefaultAsync();
                var ftf = await _context.FinTransactions.Where(p => p.ContractIdentifier == fc.IdFinAccount && (p.DebitAccount == uca || p.CreditAccount == uca)).FirstOrDefaultAsync();
                if(ftf != null) { result = ftf.TransactionValue; }
            }
            return result;
        }

        private static List<decimal> CardPayOffOptions(decimal balanceValue, decimal yearInterest, int rateFrequency)
        {
            List<decimal> result = [0, 0, 0];
            if(balanceValue == 0 || yearInterest == 0 || rateFrequency == 0) { return  result; }
            CalculatorFinance calcFin = new(yearInterest, rateFrequency)
            {
                TotalPayments = 12,
                InitialBalance = balanceValue
            };
            result[0] = calcFin.LoanFixedRate();
            calcFin.TotalPayments = 24;
            result[1] = calcFin.LoanFixedRate();
            calcFin.TotalPayments = 36;
            result[2] = calcFin.LoanFixedRate();
            return result;
        }
    }
}
