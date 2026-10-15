using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.Data.Common;

namespace MyFinances.Pages.SpecialContracts.InvestFundPages
{
    public class DeleteIFContModel(AppDBContext context, IMyDetailsService detailsService) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IMyDetailsService _details = detailsService;

        [BindProperty]
        public Guid IdContract { private set; get; } = Guid.Empty;
        [BindProperty]
        public bool ContractHasTransactions { private set; get; } = true;
        public bool ContractWithOverdues { private set; get; } = true;
        [BindProperty]
        public decimal LastRealBalance { private set; get; } = 0;
        [BindProperty]
        public bool UsedInContracts { private set; get; } = true;

        public FinContract MyContract { private set; get; } = default!;

        public string ContractAccountName { set; get; } = string.Empty;
        public string ContractorFullName { set; get; } = string.Empty;

        public List<string> CNamesWhereUsed { set; get; } = [];

        public async Task<IActionResult> OnGet(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            if (!Guid.TryParse(id, out _)) { return NotFound(); }
            IdContract = Guid.Parse(id);
            var mc = await _context.FinContracts.FindAsync(IdContract);
            var imf = await _context.InvestFunds.FindAsync(IdContract);
            if (mc is null || imf is null)
            {
                return NotFound();
            }
            ViewData["Title"] = $"Delete / terminate investment fund contract name \n {mc.ContractName}";
            MyContract = mc;
            ContractAccountName = await _details.GetAccountNameByID(MyContract.IdFinAccount);
            ContractorFullName = await _details.GetUserNameByID(MyContract.IdContractor);
            CNamesWhereUsed = await GetContractsUsingThis();
            UsedInContracts = CNamesWhereUsed.Count > 0;
            // check if contract has any transactions recorded
            FinContractHelper fch = new(_context, default!);
            ContractHasTransactions = await fch.GetContractExecutionState(IdContract);
            if (ContractHasTransactions)
            {
                ContractWithOverdues = await ContractHasOverdueTransactions(mc.IdFinAccount);
                LastRealBalance = await fch.GetContractRealBalance(mc.IdFinAccount, DateTime.Today);
            }
            return Page();
        }

        private async Task<bool> ContractHasOverdueTransactions(int idfinacc)
        {
            return await _context.PlanTransactions.Where(p => p.TransactionDate <= DateTime.Today && !p.ExecutedTransaction && p.ContractIdentifier == idfinacc).AnyAsync();
        }

        private async Task<List<string>> GetContractsUsingThis()
        {
            List<string> lcu = [];
            var deposits = await _context.FinContracts.Where(p => p.IsActiveContract && (p.IdPayAccount == MyContract.IdFinAccount || p.IdBankAccount == MyContract.IdFinAccount)).Select(s => s.ContractName).ToListAsync();
            if (deposits.Count > 0) { lcu.AddRange(deposits); }
            var aec = await _context.FinContracts
                .Join(_context.Employments, fc => fc.IdFinContract, em => em.IdEmployment, (fc, em) => new { fc.ContractName, fc.IsActiveContract, Super = em.IdSuperAccount })
                .Where(p => p.IsActiveContract && p.Super == MyContract.IdFinAccount)
                .Select(s => s.ContractName)
                .ToListAsync();
            if (aec.Count > 0) { lcu.AddRange(aec); }
            return lcu;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var mc = await _context.FinContracts.FindAsync(IdContract);
            if (mc is not null)
            {
                mc.ClosedDate = DateTime.Now;
                mc.IsActiveContract = false;
                _context.FinContracts.Update(mc);
                if (ContractHasTransactions)
                {
                    // new plan transaction with the transfer of the balance to the principal receivable account
                    // get principal receivable
                    var prec = await _context.UserContracts.Where(p => p.IdUser == MyContract.IdContractor).Select(s => s.UserReceivable).FirstOrDefaultAsync();
                    PlanTransaction tbal = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(DateTime.Now),
                        ContractIdentifier = MyContract.IdFinAccount,
                        TransactionDate = DateTime.Now,
                        DebitAccount = prec,
                        CreditAccount = MyContract.IdFinAccount,
                        TransactionValue = LastRealBalance,
                        TransactionDetail = "Transfer of balance",
                        ExecutedTransaction = false,
                        IdSource = 0
                    };
                    _context.PlanTransactions.Add(tbal);
                    // delete all plan transactions with the state of non-executed
                    var ptlist = await _context.PlanTransactions.Where(p => p.ContractIdentifier == MyContract.IdFinAccount && !p.ExecutedTransaction).ToListAsync();
                    if (ptlist.Count > 0)
                    {
                        _context.PlanTransactions.RemoveRange(ptlist);
                    }
                }
                else
                {
                    //delete all plan transactions
                    var ptlist = await _context.PlanTransactions.Where(p => p.ContractIdentifier == MyContract.IdFinAccount).ToListAsync();
                    if (ptlist.Count > 0)
                    {
                        _context.PlanTransactions.RemoveRange(ptlist);
                    }
                }
                try
                {
                    int x = await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Contract {MyContract.ContractName} successfully terminated. \n A total of {x} transactions effected.";
                }
                catch (DbException dex)
                {
                    TempData["ErrorMessage"] = $"Ooops there was an error in terminating the contract. \n {dex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = $"Ooops there was an error with retrieving the contract data. \n Try again later or contact systems administrator.";
            }
            return RedirectToPage("../IndexSpecialContracts");
        }
    }
}
