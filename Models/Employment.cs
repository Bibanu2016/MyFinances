using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("employment")]
    public class Employment: IEquatable<object>, IValidatableObject
    {
        [Key, Column("idemployment"), Display(Name = "ID")] public Guid IdEmployment { get; set; }
        [Required, Column("grosspaidcycle"), Display(Name = "Gross Pay"), DataType(DataType.Currency), Range(100, 1000000, ErrorMessage = "A value between {1} and {2} is required")] public decimal GrossPaidCycle { get; set; } = decimal.Zero;
        [Required, Column("taxpaidcycle"), Display(Name = "Tax Paid"), DataType(DataType.Currency), Range(0, 1000000, ErrorMessage = "A value between {1} and {2} is required")] public decimal TaxPaidCycle { get; set; } = decimal.Zero;
        [Required, Column("superpaidcycle"), Display(Name = "Super Paid"), DataType(DataType.Currency), Range(0, 1000000, ErrorMessage = "A value between {1} and {2} is required")] public decimal SuperPaidCycle { get; set; } = decimal.Zero;
        [Column("idsuperaccount"), Display(Name = "Super account")] public int IdSuperAccount { get; set; } = 0;

        public override int GetHashCode()
        {
            return HashCode.Combine(GrossPaidCycle, TaxPaidCycle, SuperPaidCycle, IdSuperAccount);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            else if (obj is Employment other)
            {
                int result = 1;
                result = GrossPaidCycle == other.GrossPaidCycle ? 1 : 0;
                result = TaxPaidCycle == other.TaxPaidCycle ? result * 1 : 0;
                result = SuperPaidCycle == other.SuperPaidCycle ? result * 1 : 0;
                result = IdSuperAccount == other.IdSuperAccount ? result * 1 : 0;
                return result == 1;
            }
            else
            {
                return false;
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(TaxPaidCycle + SuperPaidCycle > GrossPaidCycle) { yield return new ValidationResult("Gross paid value shall be greater than the outgoing combined value",[nameof(GrossPaidCycle)]); }
            if(SuperPaidCycle > 0 && !IdSuperAccount.ToString().StartsWith('1')) { yield return new ValidationResult("A valid superannuation account is required", [nameof(IdSuperAccount)]); }
        }
    }
}
