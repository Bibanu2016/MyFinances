using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace MyFinances.Models
{
    [Table("loan")]
    public class Loan : IEquatable<object>
    {
        [Key, Column("idloan"), Display(Name = "ID")]
        public required Guid IdLoan { get; set; }
        [Required, Column("fixrate"), Display(Name = "Is Variable Rate?")]
        public bool IdLoanType { get; set; } = true;
        [Required, NotNull, Column("loanamount"), Display(Name = "Loan amount"), DataType(DataType.Currency), Range(100, 1000000000, ErrorMessage = "A value between {1} and {2} is required")]
        public decimal LoanAmount { get; set; } = decimal.Zero;
        [Required, Column("loaninterest"), Display(Name = "Annual interest rate [%]")]
        [RegularExpression(@"^[0-9]{1,2}(\.[0-9]{1,2})$", ErrorMessage = "A value between 0 and 99.99 is required")]
        public decimal LoanInterest { get; set; } = decimal.Zero;
        [Column("loansecurity"), Display(Name = "Security asset")]
        public Guid? LoanSecurity { get; set; } = Guid.Empty;
        [Required, Column("idaccountinterest"), Display(Name = "Interest account")]
        public int IdAccountInterest { get; set; } = 0;
        [Required, Column("extrapay"), Display(Name = "Extra pay"), DataType(DataType.Currency), Range(0, 1000000, ErrorMessage = "A value between {1} and {2} is required")]
        public decimal ExtraPay { get; set; } = decimal.Zero;

        public override int GetHashCode()
        {
            return HashCode.Combine(IdLoanType, LoanAmount, LoanInterest, LoanSecurity, ExtraPay);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }
            else if (obj.GetType() != this.GetType())
            {
                return false;
            }
            else
            {
                int result = 1;
                Loan other = (Loan)obj;
                result = LoanAmount == other.LoanAmount ? result * 1 : 0;
                result = IdLoanType == other.IdLoanType ? result * 1 : 0;
                result = LoanInterest == other.LoanInterest ? result * 1 : 0;
                result = LoanSecurity == other.LoanSecurity ? result * 1 : 0;
                result = ExtraPay == other.ExtraPay ? result * 1 : 0;
                return result == 1;
            }
        }
    }
}
