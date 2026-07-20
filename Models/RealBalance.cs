using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("realbalance")]
    public class RealBalance
    {
        [Key, Column("idrealbalance")] public long IdRealBalance { get; set; } = default;
        [Required, Column("idfinaccount")] public int IdFinAccount { set; get; } = 0;
        [Required, Column("balancedate")] public DateTime BalanceDate { get; set; } = DateTime.Parse("2001-01-01");
        [Required, Column("balancevalue")] public decimal BalanceValue { get; set; } = 0.00m;
        [Required, Column("cashoutvalue")] public decimal CashOutValue { get; set; } = 0.00m;
    }
}
