using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using MyFinances.ViewModels;
using System.Data.Common;

namespace MyFinances.Pages.Contracts
{
    public class ContractDeleteModel(AppDBContext context, IConfiguration configuration, IMyDetailsService detailsService) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMyDetailsService _detailsService = detailsService;

        public ContractFull ContractFull { get; set; } = default!;

        public FinContract Contract { get; set; } = default!;
        [BindProperty]
        public decimal LastBalance { get; set; } = 0;
        [BindProperty]
        public DateTime BalanceDate { get; set; } = DateTime.Now;

        [BindProperty]
        public bool HasTransactions { get; set; } = true;
        public FinContractNames ContractNames { get; set; } = new FinContractNames();

        [BindProperty]
        public Guid IdContract { get; set; } = Guid.Empty;

        [BindProperty]
        public DateTime TerminationDate { get; set; } = DateTime.Now;

        public async Task<IActionResult> OnGetAsync(Guid? pid)
        {
            if (pid == null || pid == Guid.Empty)
            {
                return NotFound();
            }
            IdContract = pid.Value;
            Contract = await _context.FinContracts.FindAsync(IdContract) ?? default!;
            if (Contract == default) { return NotFound(); }
            ViewData["Title"] = $"Delete contract {Contract.ContractName}";
            FinContractHelper helper = new(_context, _configuration);
            HasTransactions = await helper.GetContractExecutionState(IdContract);
            ViewData["Confirmation"] = HasTransactions ? "Are you sure want to close this contract?" : "Are you sure want to delete this contract";
            await GetContractNames();
            LastBalance = await helper.GetContractRealBalance(Contract.IdFinAccount, DateTime.Now);
            if (Contract.ContractType == 3)
            {
                int cashAcc = await _context.CreditCards.Where(p => p.IdCreditCard == IdContract).Select(s => s.CashOutAccount).FirstOrDefaultAsync();
                LastBalance += await helper.GetContractRealBalance(Contract.IdFinAccount, DateTime.Now, cashAcc);
            }
            BalanceDate = await helper.GetDateLastTransac(HasTransactions, Contract.IdFinAccount, DateTime.Now);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string omsg;
            int contIdentify = 0;
            if (HasTransactions)
            {
                omsg = await TerminateContract();
            }
            else
            {
                omsg = await DeleteContract();
                // delete all financial account
                FinContractHelper fch = new(_context, _configuration);
                List<int> faList = await fch.GetContractAccounts(IdContract);
                if (faList.Count > 0)
                {
                    contIdentify = faList[0];
                    // delete all cotract's accounts
                    foreach (int k in faList)
                    {
                        var fa = await _context.FinAccounts.FindAsync(k);
                        if (fa != null) { _context.FinAccounts.Remove(fa); }
                    }
                    // delete all plan transactions
                    List<PlanTransaction> ptList = await _context.PlanTransactions.Where(p => p.ContractIdentifier == contIdentify).ToListAsync();
                    if (ptList.Count > 0)
                    {
                        foreach (PlanTransaction pt in ptList) { _context.PlanTransactions.Remove(pt); }
                    }
                    else
                    {
                        omsg += "\n Error in retrieving the contract planned transactions";
                    }
                }
                else
                {
                    omsg += "\n Error in retrieving contract accounts";
                }
            }
            if (omsg.Length > 0)
            {
                TempData["ErrorMessage"] = omsg;
                return RedirectToAction("Get", new { pid = IdContract });
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int x = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Contract has been successfuly updated! \n A total of {x} records affected.";
            }
            catch (DbException ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error during saving to the database. \n {ex.Message}";
            }
            return RedirectToPage("./ContractsList");
        }

        private async Task<string> DeleteContract()
        {
            string result = string.Empty;
            var contract = await _context.FinContracts.FindAsync(IdContract);
            if (contract is null) { return $"Error in retrieving contract id {IdContract}"; }
            bool found = false;
            switch (contract.ContractType)
            {
                case 1:
                    var sc_1 = await _context.Deposits.FindAsync(IdContract);
                    if (sc_1 != null)
                    {
                        _context.Remove(sc_1);
                        found = true;
                    }
                    break;
                case 2:
                    var sc_2 = await _context.Loans.FindAsync(IdContract);
                    if (sc_2 != null)
                    {
                        _context.Remove(sc_2);
                        found = true;

                    }
                    break;
                case 3:
                    var sc_3 = await _context.CreditCards.FindAsync(IdContract);
                    if (sc_3 != null)
                    {
                        _context.Remove(sc_3);
                        found = true;

                    }
                    break;
                case 4:
                    var sc_4 = await _context.Employments.FindAsync(IdContract);
                    if (sc_4 != null)
                    {
                        _context.Remove(sc_4);
                        found = true;

                    }
                    break;
                case 5:
                    var sc_5 = await _context.Incomes.FindAsync(IdContract);
                    if (sc_5 != null)
                    {
                        _context.Remove(sc_5);
                        found = true;

                    }
                    break;
                case 6 or 7:
                    var sc_6 = await _context.Bills.FindAsync(IdContract);
                    if (sc_6 != null)
                    {
                        _context.Remove(sc_6);
                        found = true;

                    }
                    break;
            }
            if (found == false) { return $"Error in retrieving supplement for contract id {IdContract}"; }
            _context.Remove(contract);
            return result;
        }

        private async Task<string> TerminateContract()
        {
            string result = string.Empty;
            FinContract fc = await _context.FinContracts.FindAsync(IdContract) ?? default!;
            if (fc == default) { return $"Error in retrieving contract id {IdContract}"; }
            // update closed date and contract status
            fc.ClosedDate = BalanceDate;
            fc.IsActiveContract = false;
            fc.EquivMonthlyRate = 0;
            _context.FinContracts.Update(fc);
            // delete all planned transactions after BalanceDate
            var ptList = await _context.PlanTransactions.Where(p => p.ContractIdentifier == fc.IdFinAccount && p.TransactionDate > BalanceDate).ToListAsync();
            if (ptList.Count > 0)
            {
                _context.PlanTransactions.RemoveRange(ptList);
            }
            // if has a balance to transfer, transfer it to user's payable or receivable account with the BalanceDate
            if (LastBalance > 0)
            {
                int urecAcc = await _context.UserContracts.Where(p => p.IdUser == fc.IdContractor).Select(s => s.UserReceivable).FirstOrDefaultAsync();
                PlanTransaction toRec = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(BalanceDate),
                    TransactionDate = BalanceDate,
                    TransactionValue = LastBalance,
                    DebitAccount = urecAcc,
                    CreditAccount = fc.IdFinAccount,
                    TransactionDetail = _configuration.GetSection("TransactionDetail").GetValue<string>("Close") ?? "Close account " + fc.ContractName,
                    ContractIdentifier = fc.IdFinAccount,
                    IdSource = 0
                };
                _context.PlanTransactions.Add(toRec);
            }
            if (LastBalance < 0)
            {
                int urecAcc = await _context.UserContracts.Where(p => p.IdUser == fc.IdContractor).Select(s => s.UserPayable).FirstOrDefaultAsync();
                PlanTransaction toPay = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(BalanceDate),
                    TransactionDate = BalanceDate,
                    TransactionValue = LastBalance,
                    DebitAccount = fc.IdFinAccount,
                    CreditAccount = urecAcc,
                    TransactionDetail = _configuration.GetSection("TransactionDetail").GetValue<string>("Close") ?? "Close account " + fc.ContractName,
                    ContractIdentifier = fc.IdFinAccount,
                    IdSource = 0
                };
                _context.PlanTransactions.Add(toPay);
            }
            return result;
        }

        private async Task GetContractNames()
        {
            if (Contract == default) { return; } // guard agains any errors
            ContractNames.ContractTypeName = await _detailsService.GetContractTypeByID(Contract.ContractType);
            ContractNames.ContractortName = await _detailsService.GetUserNameByID(Contract.IdContractor);
            ContractNames.FrequencyName = await _detailsService.GetFrequencyNameByID(Contract.IdFrequency);
            ContractNames.OrganisationName = await _detailsService.GetOrganisationNameByID(Contract.IdOrganisation);
            ContractNames.EndActionName = await _detailsService.GetEndActionNameByID(Contract.IdEndAction);
            return;
        }

    }
}
