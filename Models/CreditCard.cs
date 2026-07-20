using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("creditcard")]
    public class CreditCard
    {
        [Key, Column("idcreditcard"), Display(Name = "ID")] public required Guid IdCreditCard { get; set; }
        [Required, Column("creditcardlimit"), Display(Name = "Card limit"), DataType(DataType.Currency), Range(500, 1000000, ErrorMessage = "A value between {1} and {2} is required")] public int CreditCardLimit { get; set; } = 0;
        [Required, Column("creditcardinterest"), Range(0, 30, ErrorMessage = "The value should be in the range {1}% to {2}%"), Display(Name = "Annual purchase interest [%]")] public decimal CreditCardInterest { get; set; } = 0;
        [Required, Column("creditcardpaypercent"), Range(1, 10, ErrorMessage = "The value should be in the range 1% to 10%"), Display(Name = "Minimum payment [%]")] public double CreditCardPayPercent { get; set; } = 3;
        [Required, Column("cardfeepercycle"), Display(Name = "Fee per cycle"), DataType(DataType.Currency), Range(0, 100000, ErrorMessage = "A value between {1} and {2} is required")] public decimal CycleFee { get; set; } = 0;
        [Required, Column("feeaccount"), Display(Name = "Fee account")] public int FeeAccount { get; set; } = 0;
        [Required, Column("creditcardinsurancerate"), Range(0, 5, ErrorMessage = "The value should be in range 0% to 5%"), Display(Name = "Insurance rate [%]")] public decimal CreditCardInsuranceRate { get; set; } = 0;
        [Required, Column("interestaccount"), Display(Name = "Interest account")] public int InterestAccount { get; set; } = 0;
        [Required, Column("interestcashacc"), Display(Name = "Interest cash out acc")] public int InterestCashOutAcc { get; set; } = 0;
        [Required, Column("insuranceaccount"), Display(Name = "Insurance account")] public int InsuranceAccount { get; set; } = 0;
        [Required, Column("cashaccount"), Display(Name ="Cash out account")] public int CashOutAccount { get; set; } = 0;
        [Required, Column("cashinterestrate"), Range(0, 50, ErrorMessage = "The value should be in the range 0% to 30%"), Display(Name = "Annual cash interest [%]")] public decimal CashRate { get; set; } = 0;
        [Required, Column("extrapaypercycle"), DataType(DataType.Currency), Display(Name = "Extra pay"), Range(0, 1000000, ErrorMessage = "A value between {1} and {2} is required")] public decimal ExtraPay { get; set; } = 0;

        public override int GetHashCode()
        {
            return HashCode.Combine(CreditCardLimit, 
                CreditCardInterest,
                CreditCardPayPercent,
                CycleFee,
                CreditCardInsuranceRate,
                CashRate,
                ExtraPay
                );
        }

        public override bool Equals(object? obj)
        {
            if(obj is null)
            {
                return false;
            }
            else if(obj is CreditCard other)
            {
                int result = 1;
                result = CreditCardLimit == other.CreditCardLimit ? result * 1 : 0;
                result = CreditCardInterest == other.CreditCardInterest ? result * 1 : 0;
                result = CreditCardPayPercent == other.CreditCardPayPercent ? result * 1 : 0;
                result = CycleFee == other.CycleFee ? result * 1 : 0;
                result = CreditCardInsuranceRate == other.CreditCardInsuranceRate ? result * 1 : 0;
                result = CashRate == other.CashRate ? result * 1 : 0;
                result = ExtraPay == other.ExtraPay ? result * 1 : 0;
                return result == 1;
            }
            else
            {
                return false;
            }
        }

    }
}
