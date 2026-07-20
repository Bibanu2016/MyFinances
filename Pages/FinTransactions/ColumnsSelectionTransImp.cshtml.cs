using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Services;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MyFinances.Pages.FinTransactions
{
    public class ColumnsSelectionTransImpModel(AppDBContext dbContext, IMyCsvService myCsvService, IMemoryCache cache) : PageModel
    {
        private readonly AppDBContext _dbContext = dbContext;
        private readonly IMyCsvService _myCsvService = myCsvService;
        private readonly IMemoryCache _cache = cache;

        [BindProperty]
        public DataTable ImportTable { get; set; } = new DataTable();

        [BindProperty]
        public int AccountNumber { get; set; }

        public int DisplayRows = 5;

        [BindProperty, Required, Length(1, 1, ErrorMessage = "A value of one character is required")]
        public string DateColumn { get; set; } = string.Empty;
        [BindProperty, Required, Length(1, 1, ErrorMessage = "A value of one character is required")]
        public string AmountColumn { get; set; } = string.Empty;
        [BindProperty, Required, Length(1, 1, ErrorMessage = "A value of one character is required")]
        public string DetailsColumn { get; set; } = string.Empty;

        public SelectList DateColumns { get; set; } = default!;
        public SelectList AmountColumns { get; set; } = default!;
        public SelectList DetailsColumns { get; set; } = default!;


        public void OnGet(int accid = 0)
        {
            // initialize list elements
            List<SelectListItem> datesCols = [];
            List<SelectListItem> amountCols = [];
            List<SelectListItem> detailsCols = [];
            AccountNumber = accid;
            // get data
            if (HttpContext.Session.TryGetValue("TIImport", out byte[]? data))
            {
                if (data.Length > 0)
                {
                    string result = Encoding.UTF8.GetString(data);
                    ImportTable = _myCsvService.CsvToDataTable(result);
                    if(ImportTable.Rows.Count < 5) { DisplayRows = ImportTable.Rows.Count; }
                    DataRow row = ImportTable.Rows[0];
                    if (row != null)
                    {
                        for (int j = 0; j < ImportTable.Columns.Count; j++)
                        {
                            if (DateTime.TryParse(row[j].ToString(), out _)) { datesCols.Add(new SelectListItem() { Text = ImportTable.Columns[j].ColumnName, Value = j.ToString() }); }
                            else if(Decimal.TryParse(row[j].ToString(), out _)) { amountCols.Add(new SelectListItem() { Text = ImportTable.Columns[j].ColumnName, Value = j.ToString() }); }
                            else {  detailsCols.Add(new SelectListItem() { Text = ImportTable.Columns[j].ColumnName, Value = j.ToString() }); }
                        }
                        DateColumns = new SelectList(datesCols, nameof(SelectListItem.Value), nameof(SelectListItem.Text));
                        DetailsColumns =  new SelectList(detailsCols, nameof(SelectListItem.Value), nameof(SelectListItem.Text));
                        AmountColumns = new SelectList(amountCols, nameof(SelectListItem.Value), nameof(SelectListItem.Text));
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "No data has been received";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "No data has been received";
            }
           
            //create Dictionary - column number and value from selected list
            // fill out dictionary based on evaluation of the types for each column
            // if only one type per table - set all disable
            return;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // validate
            if(!ModelState.IsValid)
            {
                return Page();
            }
            if (HttpContext.Session.TryGetValue("TIImport", out byte[]? data))
            {
                if (data.Length > 0)
                {
                    string result = Encoding.UTF8.GetString(data);
                    ImportTable = _myCsvService.CsvToDataTable(result);
                }
            }
            if (ImportTable.Rows.Count > 0)
            {
                await CleanExistingFromImport();
                string outcome = await RecordImported();
                if (outcome.StartsWith("A"))
                {
                    TempData["SuccessMessage"] = outcome + $"\n for account number {AccountNumber}";
                }
                else
                {
                    TempData["ErrorMessage"] = outcome;
                }
            }
            else
            {
                TempData["ErrorMessage"] = $"An error occured with retrievingt the imported values";
            }
            HttpContext.Session.Remove("TIImport");
            //redirect to page UnpairedTransactions
            return RedirectToPage("IndexFinTransactions");
        }

        private async Task CleanExistingFromImport()
        {
            int idxDate = int.Parse(DateColumn);
            int idxAmount = int.Parse(AmountColumn);
            int idxDetails = int.Parse(DetailsColumn);
            //get the first and latest date within the transaction range
            DateTime earliestDate = DateTime.Today;
            DateTime latestDate = DateTime.Today;
            foreach (DataRow row in ImportTable.Rows)
            {
                var df = row.ItemArray[idxDate];
                var da = row.ItemArray[idxAmount];
                var dd = row.ItemArray[idxDetails];
                if (df == null || da == null || dd == null)
                {
                    continue;
                }
                if (DateTime.TryParse(df.ToString(), out DateTime converted))
                {
                    if (earliestDate > converted)
                    {
                        earliestDate = converted;
                    }
                    if(latestDate < converted)
                    {
                        latestDate = converted;
                    }
                }
            }
            // get existing transactions withing the date range
            var tlwdr = await _dbContext.FinTransactions.Where(p => (p.TransactionDate >= earliestDate && p.TransactionDate <= latestDate) && (p.DebitAccount == AccountNumber || p.CreditAccount == AccountNumber)).AsNoTracking().ToListAsync();
            if (tlwdr.Count > 0)
            {
                FinTransaction temp = new() { IdTransaction = 0};
                //remove from ImportTable duplicate by date, amount, transaction detail - limit 64
                foreach(FinTransaction ft in tlwdr)
                {
                    int signAmount = ft.DebitAccount == AccountNumber ? 1 : -1;
                    foreach(DataRow row in ImportTable.Rows)
                    {
                        temp.TransactionDate = DateTime.Parse(row[0].ToString() ?? "2001-01-01");
                        // check only for date and value as it is unlikely to have the exact value for the same date with the same value
                        if(row.Field<string>(idxDate) == ft.TransactionDate.ToShortDateString() && row.Field<string>(idxAmount) == (signAmount * ft.TransactionValue).ToString())
                        {
                            ImportTable.Rows.Remove(row);
                            break;
                        }
                    }
                }

            }
            return;
        }

        private async Task<string> RecordImported()
        {
            int idxDate = int.Parse(DateColumn);
            int idxAmount = int.Parse(AmountColumn);
            int idxDetails = int.Parse(DetailsColumn);
            // get the contract number
            var contract = await _dbContext.FinContracts.AsQueryable().Where(p=>p.IdFinAccount == AccountNumber).AsNoTracking().FirstOrDefaultAsync();
            int idContract = 0;
            if (contract != null) { idContract = contract.IdFinAccount; }
            Dictionary<int, string> keysT = await GetKeysForDictionary(AccountNumber);
            List<FinTransaction> ftList = [];
            //check if the header contains data
            string[] headerStr =
            [
                ImportTable.Columns[idxDate].ColumnName,
                ImportTable.Columns[idxAmount].ColumnName,
                ImportTable.Columns[idxDetails].ColumnName,
            ];
            if (DateTime.TryParse(headerStr[0], out DateTime hDate) && Decimal.TryParse(headerStr[1], out decimal hAmount))
            {
                int targetAccount = 0;
                foreach (var di in keysT)
                {
                    if (headerStr[2].Contains(di.Value))
                    {
                        targetAccount = di.Key;
                        break;
                    }
                }
                FinTransaction newft = new()
                {
                    IdTransaction = GeneratorID.Int64IDBasedOnDate(hDate),
                    TransactionDate = hDate,
                    DebitAccount = hAmount >= 0 ? AccountNumber : targetAccount,
                    CreditAccount = hAmount < 0 ? AccountNumber : targetAccount,
                    TransactionValue = Math.Abs(hAmount),
                    TransactionDetail = headerStr[2],
                    ContractIdentifier = idContract,
                    IdSource = AccountNumber
                };
                _dbContext.FinTransactions.Add(newft);
            }
            foreach (DataRow row in ImportTable.Rows)
            {
                
                if(row.Field<string>(idxDate) != null && row.Field<string>(idxAmount) != null && row.Field<string>(idxDetails) != null)
                {
                    DateTime date = DateTime.Parse(row.Field<string>(idxDate) ?? "01/01/2001");
                    decimal tValue = Decimal.Parse(row.Field<string>(idxAmount) ?? "0");
                    string tDetails = row.Field<string>(idxDetails) ?? string.Empty;
                    if(tDetails.Length > 64) tDetails = tDetails[..64];
                    //get the target account from the dictionary
                    int targetAccount = 0;
                    foreach (var di in keysT)
                    {
                        if (tDetails.Contains(di.Value))
                        {
                            targetAccount = di.Key; 
                            break;
                        }
                    }
                    FinTransaction newft = new()
                    {
                        IdTransaction = GeneratorID.Int64IDBasedOnDate(date),
                        TransactionDate = date,
                        DebitAccount = tValue >= 0 ? AccountNumber : targetAccount,
                        CreditAccount = tValue < 0 ? AccountNumber : targetAccount,
                        TransactionValue = Math.Abs(tValue),
                        TransactionDetail = tDetails,
                        ContractIdentifier = idContract,
                        IdSource = AccountNumber
                    };
                    _dbContext.FinTransactions.Add(newft);
                    ftList.Add(newft);
                }
            }
            
            //update Overdue transactions
            if (ftList.Count > 0)
            {
                //get the start date and end date for the transactions
                DateTime fromDate = ftList[0].TransactionDate;
                DateTime toDate = ftList[^1].TransactionDate;
                for (int i = 1; i < ftList.Count - 1; i++)
                {
                    if (ftList[i].TransactionDate < fromDate) {fromDate = ftList[i].TransactionDate;}
                    if (ftList[i].TransactionDate > toDate) { toDate = ftList[i].TransactionDate;}
                }
                var ptl = await _dbContext.PlanTransactions.AsQueryable().Where(p=>p.TransactionDate >= fromDate && p.TransactionDate <= toDate && (p.DebitAccount == AccountNumber || p.CreditAccount == AccountNumber)).ToListAsync();
                if (ptl.Count > 0)
                {
                    decimal difAmount = 0;
                    int difDate = 0;
                    decimal refValue = 0;
                    foreach (var pt in ptl)
                    {
                        foreach (var ft in ftList)
                        {
                            if(pt.DebitAccount == ft.DebitAccount && pt.CreditAccount == ft.CreditAccount)
                            {
                                difAmount = Math.Abs(pt.TransactionValue - ft.TransactionValue);
                                difDate = Math.Abs((pt.TransactionDate - ft.TransactionDate).Days);
                                refValue = ft.TransactionValue * 0.1m;
                                if((difAmount <= refValue) && difDate <= 2) 
                                { 
                                    pt.ExecutedTransaction = true;
                                    _dbContext.PlanTransactions.Update(pt);
                                    break; 
                                }
                            }
                        }
                    }
                }

            }
            string result;
            try
            {
                int tnr = await _dbContext.SaveChangesAsync();
                result = $"A total of {tnr} transactions have been imported and recorded";
            }
            catch (DbException ex)
            {
                result = $"Ooops .. an error occured while trying to save imported transactions. \n {ex.Message}";
            }
            return result;
        }
        private async Task<Dictionary<int, string>> GetKeysForDictionary(int sourceID)
        {
            Dictionary<int, string> result = [];
            if (sourceID == 0) return result;
            var extract = await _dbContext.TransacDictionary.Where(p => p.IdSource == sourceID).ToListAsync();
            if (extract.Count > 0)
            {
                foreach (var kvp in extract)
                {
                    if(kvp.IdTarget != 0 && !result.ContainsKey(kvp.IdTarget)) result.Add(kvp.IdTarget, kvp.Name);
                }
            }
            return result;
        }

        



    }
}
