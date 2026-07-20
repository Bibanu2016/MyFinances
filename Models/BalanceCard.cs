using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("balancecard")]
    public class BalanceCard
    {
        [Key, Column("idbalancecard")] public long IdBalance { get; set; } = 0;
        [Required, Column("idfinaccount")] public int IdAccount { get; set; } = 0;
        [Required, Column("balancedate"), DataType(DataType.Date)] public DateTime DateBalance { get; set; } = DateTime.MinValue;
        [Required, Column("purchasebalance")] public decimal PurchaseBalance { get; set; } = 0m;
        [Required, Column("cashbalance")] public decimal CashlBalance { get; set; } = 0m;
        public decimal TotalBalance { get { return PurchaseBalance + CashlBalance; } }
    }
}
