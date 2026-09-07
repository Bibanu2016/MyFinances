using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace MyFinances.Models
{
    [Table("fincontract")]
    public class FinContract: IEquatable<object>, IValidatableObject
    {
        [Key, Column("idfincontract"), Display(Name = "ID Contract")] public required Guid IdFinContract { get; set; }
        [Required, Column("idcontracttype"), Display(Name ="Type contract")] public short ContractType { get; set; } = 0;
        [Required, Column("fincontractname"), MinLength(3, ErrorMessage = "Minimum 3 characters are required!"), MaxLength(40, ErrorMessage = "Maximum 40 characters are allowed!"), Display(Name = "Name")] public string ContractName { get; set; } = string.Empty;
        [Required, Column("fincontractdetails"), MaxLength(255, ErrorMessage = "Maximum of 255 characters are allowed!"), Display(Name = "Details")] public string ContractDetails { get; set; } = string.Empty;
        [Required, Column("idorganisation"), Display(Name = "Organisation id")] public int IdOrganisation { get; set; } = 0;
        [Required, Column("idcontractor"), Display(Name = "Contractor id")] public short IdContractor { get; set; } = 0;
        [Required, Column("fincontractstartdate"), DataType(DataType.Date), Display(Name = "Start date")] public DateTime StartDate { get; set; } = DateTime.Parse("2001-01-01");
        [Required, Column("fincontractenddate"), DataType(DataType.Date), Display(Name = "End date")] public DateTime EndDate { get; set; } = DateTime.MaxValue;
        [Required, Column("idendaction"), Display(Name = "End action")] public short IdEndAction { get; set; } = 0;
        [Required, Column("invoicedate"), DataType(DataType.Date), Display(Name = "Invoice date")] public DateTime InvoiceDate { get; set; } = DateTime.Today;
        [Required, Column("invoicenetdays"), Display(Name = "Net days"), Range(0, 180, ErrorMessage ="A value in the range {1} to {2} is required")] public short InvoiceNetDays { get; set; } = 0;
        [Required, Column("idfrequency"), Display(Name = "Payment frequency")] public short IdFrequency { get; set; } = 0;
        [Required, Column("idfinaccount"), Display(Name = "Contract account")] public int IdFinAccount { get; set; } = 0;
        [Required, Column("idpayaccount"), Display(Name = "Payment account")] public int IdPayAccount { get; set; } = 0;
        [Column("idbankaccount"), Display(Name = "Bank account")] public int IdBankAccount { get; set; } = 0;
        [Required, Column("fincontractactive"), Display(Name = "Is contract active")] public bool IsActiveContract { get; set; } = false;
        [Required, Column("closeddate"), DataType(DataType.Date), Display(Name = "Closed date")] public DateTime ClosedDate { get; set; } = DateTime.MaxValue.AddDays(-1);
        [Required, Column("eqmonthrate"), Display(Name = "Equiv. Monthly Rate")] public int EquivMonthlyRate { get; set; } = 0;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is null || GetType() != obj.GetType()) return false;
            var other = (FinContract)obj;
            int result = 1;
            result *= ContractType == other.ContractType ? 1 : 0;
            result *= ContractName == other.ContractName ? 1 : 0;
            result *= ContractDetails == other.ContractDetails ? 1 : 0;
            result *= IdOrganisation == other.IdOrganisation ? 1 : 0;
            result *= IdContractor == other.IdContractor ? 1 : 0;
            result *= StartDate.Date == other.StartDate.Date ? 1 : 0;
            result *= EndDate.Date == other.EndDate.Date ? 1 : 0;
            result *= IdEndAction == other.IdEndAction ? 1 : 0;
            result *= InvoiceDate.Date == other.InvoiceDate.Date ? 1 : 0;
            result *= InvoiceNetDays == other.InvoiceNetDays ? 1 : 0;
            result *= IdFrequency == other.IdFrequency ? 1 : 0;
            result *= IdPayAccount == other.IdPayAccount ? 1 : 0;
            result *= IdBankAccount == other.IdBankAccount ? 1 : 0;
            result *= IsActiveContract == other.IsActiveContract ? 1 : 0;
            result *= ClosedDate.Date == other.ClosedDate.Date ? 1 : 0;
            return result == 1;
            
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + ContractType.GetHashCode();
                hash = hash * 23 + ContractName.GetHashCode();
                hash = hash * 23 + ContractDetails.GetHashCode();
                hash = hash * 23 + IdOrganisation.GetHashCode();
                hash = hash * 23 + IdContractor.GetHashCode();
                hash = hash * 23 + StartDate.Date.GetHashCode();
                hash = hash * 23 + EndDate.Date.GetHashCode();
                hash = hash * 23 + IdEndAction.GetHashCode();
                hash = hash * 23 + InvoiceDate.Date.GetHashCode();
                hash = hash * 23 + InvoiceNetDays.GetHashCode();
                hash = hash * 23 + IdFrequency.GetHashCode();
                hash = hash * 23 + IdPayAccount.GetHashCode();
                hash = hash * 23 + IdBankAccount.GetHashCode();
                hash = hash * 23 + IsActiveContract.GetHashCode();
                hash = hash * 23 + ClosedDate.Date.GetHashCode();
                return hash;

            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(StartDate.Date > DateTime.Today.Date) { yield return new ValidationResult("A start date ealier than today is required", [nameof(StartDate)]); }
            if(EndDate.Date < DateTime.Today.Date) { yield return new ValidationResult("An end date later than or equal to today is required", [nameof(EndDate)]); }
            if (InvoiceDate.Date < DateTime.Today.Date) { yield return new ValidationResult("An invoice date later than or equal to today is required", [nameof(InvoiceDate)]); }
            else
            {
                FinanceCalculator.CalculatorDates calculatorDates = new(IdFrequency);
                DateTime ipd = calculatorDates.IncrementCycleDate(DateTime.Today);
                if (InvoiceDate.Date > ipd) { yield return new ValidationResult($"An invoice date less than {ipd:d} is required", [nameof(InvoiceDate)]); }
            }
            if(ContractType == 2 && IdEndAction != 0) { yield return new ValidationResult("Only Terminate is required for a loan", [nameof(IdEndAction)]); }
            if(IdPayAccount.ToString().StartsWith('2') && IdBankAccount == 0) { yield return new ValidationResult("A bank account is required", [nameof(IdBankAccount)]); }
        }

    }
}
