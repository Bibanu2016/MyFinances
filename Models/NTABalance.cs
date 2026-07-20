using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("ntabalance")]
    public class NTABalance
    {
        [Key, Column("idntabalance")] public long IdNTABalance { get; set; } = default;
        [Required, Column("idfinaccount")] public int IdFinAccount { set; get; } = 0;
        [Required, Column("balancedate")] public DateTime BalanceDate { get; set; } = DateTime.Parse("2001-01-01");
        [Required, Column("balancevalue")] public decimal BalanceValue { get; set; } = 0.00m;
    }
}
