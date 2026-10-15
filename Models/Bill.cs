using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("bill")]
    public class Bill : IEquatable<object>
    {
        [Key, Column("idbill"), Display(Name = "ID")] public required Guid IdBill { get; set; }
        [Required, Column("billcyclerate"), Display(Name = "Bill rate"), DataType(DataType.Currency), Range(1, 1000000000, ErrorMessage = "A value between {1} and {2} is required")] public decimal BillCycleRate { get; set; } = 0;
        public override int GetHashCode()
        {
            return HashCode.Combine(BillCycleRate);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }
            else if (obj is Bill other)
            {
                int result = BillCycleRate == other.BillCycleRate ? 1 : 0;
                return result == 1;
            }
            else { return false; }
        }

    }
}
