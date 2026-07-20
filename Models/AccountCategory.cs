using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("accountcategory")]
    public class AccountCategory
    {
        [Key, Column("idaccountcategory")] public int IdAccountCategory { set; get; } = 0;
        [Required, Column("idaccountclass")] public short IdAccountClass { set; get; } = 0;
        [Required, Column("accountcategoryname")] public string AccountCategoryName { get; set; } = "Not defined";
        [Required, Column("idcontractcategory")] public short IdContractCategory { set; get; } = 0;
        [Required, Column("idpayable")] public short IdPayable { set; get; } = 0;
        // index from right to left: 0 - asset payment, 1 - liability payment, 2 - source import transaction, 3 - investment fund
        [Required, Column("categroles")] public byte[] CategoryRoles { set; get; } = new byte[1];
    }
}
