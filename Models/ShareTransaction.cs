using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("sharetransaction")]
    public class ShareTransaction
    {
        [Key, Column("idsharetransaction")] public required Guid IdShareTransaction { get; set; }
        [Required, Column("idshare"), MaxLength(10, ErrorMessage = "Maximum 10 char is allowed"), MinLength(3, ErrorMessage = "Minimum 3 char is required")]
        public string IdShare { get; set; } = string.Empty;
        [Required, Column("transactiondate"), DataType(DataType.Date)] public DateOnly DateShareTransaction { get; set; } = DateOnly.MinValue;
        [Required, Column("shareunits")] public uint ShareUnits { get; set; } = 0;
        [Required, Column("sharevalue")] public decimal ShareUnitPrice { get; set; } = 0;
        [Required, Column("transactioncost")] public decimal TransactionCost { get; set; } = 0;
        [Required, Column("idcostaccount"), StringLength(8)] public string TransactionCostAccount { get; set; } = "ND000000";
    }
}
