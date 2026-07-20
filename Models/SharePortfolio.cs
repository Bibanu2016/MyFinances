using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("sharesportfolio")]
    public class SharePortfolio
    {
        [Key, Column("idshare"), MinLength(3)] public required string IdShare { get; set; }
        [Required, Column("idfinaccount")] public int IdFinAccount { get; set; } = 0;
        [Required, Column("unitsheld")] public int UnitsHeld { get; set; } = 0;
        [Required, Column("unitbuyvalue")] public decimal ShareBuyValue { get; set; } = 0;
        [Required, Column("sharemarketvalue")] public decimal MarketValue { get; set; } = 0;
        [Required, Column("valuationclosedate"), DataType(DataType.Date)] public DateOnly ValuationDate { get; set; } = DateOnly.MinValue;
    }
}
