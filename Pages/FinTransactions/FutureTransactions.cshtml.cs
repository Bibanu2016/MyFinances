using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinances.Pages.FinTransactions
{
    public class FutureTransactionsModel(AppDBContext context, IMemoryCache memory, IComboFillService fillService) : PageModel
    {
        private readonly AppDBContext _context = context;
        private readonly IMemoryCache _memory = memory;
        private readonly IComboFillService _fillService = fillService;
        public IList<PlanTransaction> PlanTransaction { get;set; } = [];

        public IList<PlanTransaction> IncomingList { get; set; } = [];
        public IList<PlanTransaction> OverdueList { get; set; } = [];

        public IList<PlanTransaction> ExecutedList { get; set; } = [];
        [BindProperty(SupportsGet = true)]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);
        [BindProperty(SupportsGet =true)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
        [BindProperty(SupportsGet = true)]
        public int FilterAccount { get; set; } = 0;
        public SelectList AccList { get; set; } = default!;


        public async Task OnGetAsync()
        {
            if(EndDate < DateTime.Today) { EndDate = DateTime.Today.AddDays(7); } // guard against an EndDate less than a week from today
            if(StartDate > DateTime.Today) { StartDate = DateTime.Today; }
            PlanTransaction = await ApplyFilters(_context.PlanTransactions).ToListAsync() ?? [];
            if (PlanTransaction.Count > 0)
            {
                IncomingList.AddRange([.. PlanTransaction.Where(x => x.TransactionDate > DateTime.Today)]);
                ExecutedList.AddRange([.. PlanTransaction.Where(x => x.ExecutedTransaction)]);
            }
            OverdueList = await ApplyFiltersOverdue(_context.PlanTransactions).ToListAsync() ?? [];
            AccList = await _fillService.GetFinancialAccountsList();
        }

        private IQueryable<PlanTransaction> ApplyFilters(IQueryable<PlanTransaction> query)
        {
            if(FilterAccount > 0)
            {
                query = query.Where(p => p.TransactionDate >= StartDate && p.TransactionDate < EndDate && (p.DebitAccount == FilterAccount || p.CreditAccount == FilterAccount)).OrderByDescending(y => y.TransactionDate).AsNoTracking();
            }
            else
            {
                query = query.Where(p => p.TransactionDate >= StartDate && p.TransactionDate < EndDate).OrderByDescending(y => y.TransactionDate).AsNoTracking();
            }
            
            return query;
        }

        private IQueryable<PlanTransaction> ApplyFiltersOverdue(IQueryable<PlanTransaction> query)
        {
            if (FilterAccount > 0)
            {
                query = query.Where(p => p.TransactionDate < DateTime.Today && p.ExecutedTransaction == false && (p.DebitAccount == FilterAccount || p.CreditAccount == FilterAccount)).OrderByDescending(y => y.TransactionDate).AsNoTracking();
            }
            else
            {
                query = query.Where(p => p.TransactionDate < DateTime.Today && p.ExecutedTransaction == false).OrderByDescending(y => y.TransactionDate).AsNoTracking();
            }
            return query;
        }
    }
}
