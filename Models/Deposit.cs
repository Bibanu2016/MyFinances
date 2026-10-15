using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MyFinances.Models
{
    [Table("deposit")]
    public class Deposit : IEquatable<object>
    {
        [Key, Column("iddeposit"), Display(Name = "ID")] public required Guid IdDeposit { get; set; }
        [Required, Column("depositrate"), Display(Name = "Deposit rate"), DataType(DataType.Currency), Range(20, 100000, ErrorMessage = "Value {0} shall be between {1} and {2}")] public decimal DepositRate { get; set; } = 0;
        [Required, Column("interestrate"), Display(Name = "Anual interest rate [%]"), RegularExpression(@"^[0-9]{1,2}(\.[0-9]{1,2})$", ErrorMessage = "A value between 0 and 99.99 is required")] public decimal InterestRatePerYear { get; set; } = 0;
        [Required, Column("initialbalance"), Display(Name = "Initial balance"), DataType(DataType.Currency), Range(0, 1000000000, ErrorMessage = "A value between {1} and {2} is required")] public decimal DepositInitialBalance { get; set; } = 0;
        [Required, Column("idinterestaccount"), Display(Name = "Interest account")] public int InterestAccount { get; set; } = 0;

        public override int GetHashCode()
        {
            return HashCode.Combine(IdDeposit, DepositRate, InterestRatePerYear, DepositInitialBalance, InterestAccount);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is null || GetType() != obj.GetType()) return false;
            int result = 1;
            if (obj is Deposit other)
            {
                result = IdDeposit == other.IdDeposit ? result * 1 : 0;
                result = DepositRate == other.DepositRate ? result * 1 : 0;
                result = InterestRatePerYear == other.InterestRatePerYear ? result * 1 : 0;
                result = DepositInitialBalance == other.DepositInitialBalance ? result * 1 : 0;
            }
            return result == 1;
        }
    }
}
