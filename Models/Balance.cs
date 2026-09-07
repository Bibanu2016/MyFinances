using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("balance")]
    public class Balance
    {
        [Key, Column("idbalance")] public long IdBalance { get; set; } = default!; //value is yy * 10,000,000 + dayOfTheYear * 10,0000 + (0 .. 9,999)
        [Required, Column("idfinaccount")] public int IdFinAccount { get; set; } = 0;
        [Required, Column("balancedate")] public DateTime BalanceDate { get; set; } = DateTime.MinValue;
        [Required, Column("balancevalue")] public decimal BalanceValue { get; set; } = 0m;
        [Column("cashvalue")] public decimal CashValue { get; set; } = 0m;
    }
}
