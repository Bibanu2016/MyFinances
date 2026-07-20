using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("income")]
    public class Income: IEquatable<object>, IValidatableObject
    {
        [Key, Column("idincome"), Display(Name = "ID")]
        public required Guid IdIncome { get; set; }
        [Required, Column("grosspercycle"), DisplayName("Gross pay per cycle"), DataType(DataType.Currency), Range(100, 10000000, ErrorMessage ="A value between {1} and {2} is required")]
        public decimal GrossPerCycle { get; set; } = 0;
        [Required, Column("taxpaidcycle"), Display(Name = "Tax paid per cycle"), DataType(DataType.Currency), Range(0, 10000000, ErrorMessage = "A value between {1} and {2} is required")] public decimal TaxPaidCycle { get; set; } = 0;

        public override int GetHashCode()
        {
            return HashCode.Combine(GrossPerCycle, TaxPaidCycle);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            else if (obj is Income other)
            {
                return GrossPerCycle == other.GrossPerCycle && TaxPaidCycle == other.TaxPaidCycle;
            }
            else
            {
                return false;
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(TaxPaidCycle > GrossPerCycle) {  yield return new ValidationResult("A gross paid value greater than Tax is required", [nameof(GrossPerCycle)]); }
        }
    }
}
