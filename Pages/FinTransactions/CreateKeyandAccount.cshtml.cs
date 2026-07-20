using Elfie.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using Org.BouncyCastle.Security;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Pages.FinTransactions
{
    public class CreateKeyandAccountModel(AppDBContext dbcontex, IComboFillService comboFill, IMyDetailsService detailsService, IMemoryCache memory) : PageModel
    {
        private readonly AppDBContext _appDB = dbcontex;
        private readonly IComboFillService _comboFill = comboFill;
        private readonly IMyDetailsService _myDetailsService = detailsService;
        private readonly IMemoryCache _memory = memory;

        [BindProperty]
        public FinTransaction UFinTransaction { get; set; } = default!;

        [BindProperty]
        public int IdSource { get; set; } = 0;

        [BindProperty, Required(ErrorMessage ="Value is required"), MinLength(3,ErrorMessage ="A minimum 3 char is required"), MaxLength(45, ErrorMessage ="A maximum length of 45 char is allowed")]
        public string KeyTransaction { get; set; } = string.Empty;
        [BindProperty]
        public FinAccount NewAccount { get; set; } = default!;

        public SelectList AccCategList { get; set; } = default!;
        public SelectList UserAccountList { get; set; } = default!;
        public string SourceName {  get; set; } = string.Empty ;
        
        public async Task<IActionResult> OnGet(long idTrans, int idSource)
        {
            ViewData["Title"] = $"Create new target account and transaction key for source id {idSource}";
            var uft = await _appDB.FinTransactions.FindAsync(idTrans);
            if(uft == null) { return NotFound(); }
            UFinTransaction = uft;
            IdSource = idSource;
            await FillOutData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("UFinTransaction");
            ModelState.Remove("TransactionDetail");
            string[] xl = KeyTransaction.Split(' ');
            if(xl.Length > 3) { ModelState.TryAddModelError("KeyTransation", "A maximum of 3 words are allowed"); }
            if(await KeyIsUnique(KeyTransaction) == false) { ModelState.TryAddModelError("KeyTransation", "A key or key word already exists"); }
            if(!ModelState.IsValid)
            {
                await FillOutData();
                return Page();
            }
            FinAccountHelper fah = new(_appDB, default!);
            NewAccount.IdFinAccount = (await fah.NewIdAccountByCateg(NewAccount.AccountCategory))[0];
            TransacDictionary ntd = new()
            {
                IdKey = GeneratorID.TransDictKeyId(KeyTransaction),
                IdTarget = NewAccount.IdFinAccount,
                IdSource = IdSource,
                Name = KeyTransaction
            };
            _appDB.FinAccounts.Add(NewAccount);
            _appDB.TransacDictionary.Add(ntd);
            int result = 0;
            string sucMsg = string.Empty;
            try
            {
                result = await _appDB.SaveChangesAsync();
                sucMsg = $"Account {NewAccount.Name} and Key {KeyTransaction} recorded.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ooops there was en error in saving the data to database. \n {ex.Message}";
            }
            if(result == 2)
            {
                //match the unpaired transactions to key
                var resultList = await _appDB.FinTransactions.AsQueryable().Where(p => p.DebitAccount == IdSource && p.CreditAccount == 0).ToListAsync();
                if (resultList.Count > 0)
                {
                    foreach (FinTransaction t in resultList)
                    {
                        if (t.TransactionDetail.Contains(KeyTransaction)) 
                        { 
                            t.CreditAccount = NewAccount.IdFinAccount;
                            _appDB.FinTransactions.Update(t);
                        }
                    }
                }
                resultList = await _appDB.FinTransactions.AsQueryable().Where(p => p.CreditAccount == IdSource && p.DebitAccount == 0).ToListAsync();
                if (resultList.Count > 0)
                {
                    foreach (FinTransaction t in resultList)
                    {
                        if (t.TransactionDetail.Contains(KeyTransaction))
                        {
                            t.DebitAccount = NewAccount.IdFinAccount;
                            _appDB.FinTransactions.Update(t);
                        }
                    }
                }
                int ttu = await _appDB.SaveChangesAsync();
                sucMsg = sucMsg + $"\n A total of {ttu} transactions updated.";
                TempData["SuccessMessage"] = sucMsg;
                _memory.Remove("FTL"); // force to update the transactions list when returning to Index
            }
            return RedirectToPage("IndexFinTransactions");
        }

        private async Task FillOutData()
        {
            var alist = await _memory.GetOrCreateAsync("actglist", async (u) =>
            {
                u.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                return await _comboFill.GetAllAccountCategoriesList();
            });
            if (alist != null) { AccCategList = alist; }
            var usl = await _memory.GetOrCreateAsync("userlist", async (e) =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                return await _comboFill.GetUsersList();
            });
            if (usl != null) { UserAccountList = usl; }
            SourceName = await _myDetailsService.GetAccountNameByID(IdSource);
        }

        private async Task<bool> KeyIsUnique(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if(key.Length < 3) return false;
            string[] kWords = key.Split(new char[] { ' ' });
            foreach (string word in kWords)
            {
                var found = await _appDB.TransacDictionary.Where(p=>p.Name == word && p.IdSource == IdSource).FirstOrDefaultAsync();
                if(found != null) return false;
            }
            return true;
        }
    }
}
