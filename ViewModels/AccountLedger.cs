using System.ComponentModel.DataAnnotations;

namespace MyFinances.ViewModels
{
    public class AccountLedger
    {
        [Display(Name ="ID")]
        public long Id { get; set; } = 0;
        [Display(Name = "DATE")]
        public DateOnly DateTransaction { get; set; } = new DateOnly();
        [Display(Name = "ACCOUNT")]
        public string AccountName { get; set; } = string.Empty;
        [Display(Name = "DETAILS")]
        public string Details { get; set; } = string.Empty;
        [Display(Name = "AMOUNT [AUD]")]
        public decimal Amount { get; set; } = 0;
        [Display(Name ="CONTRACT NAME")]
        public string ContractName {  get; set; } = string.Empty;
        [Display(Name ="SOURCE NAME")]
        public string SourceName {  get; set; } = string.Empty;

    }
}
