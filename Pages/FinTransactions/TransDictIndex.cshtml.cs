using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.ViewModels;

namespace MyFinances.Pages.FinTransactions
{
    public class TransDictIndexModel(AppDBContext context) : PageModel
    {
        private readonly AppDBContext _context = context;

        public IList<TransacDictionary> TransacDictionary { get; set; } = default!;

        public IList<ViewModels.ComboIntStrLVM> AccountsList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            TransacDictionary = await _context.TransacDictionary.OrderBy(o => o.IdKey).ToListAsync();
            AccountsList = await _context.FinAccounts.Where(p => p.AccountCategory >= 1000).OrderBy(o => o.IdFinAccount).Select(s => new ComboIntStrLVM { ItemID = s.IdFinAccount, Name = s.Name }).ToListAsync();
        }
    }
}
