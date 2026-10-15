using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("accountcategory")]
    public class AccountCategory
    {
        [Key, Column("idaccountcategory"), Display(Name = "ID")] public int IdAccountCategory { set; get; } = 0;
        [Required, Column("idaccountclass"), Display(Name = "Account class")] public short IdAccountClass { set; get; } = 0;
        [Required, Column("accountcategoryname"), Display(Name = "Name")] public string AccountCategoryName { get; set; } = "Not defined";
        [Required, Column("idcontractcategory"), Display(Name = "Contract category")] public short IdContractCategory { set; get; } = 0;
        // Bit 0 - Non-tangible asset for payment, 1 - credit card or other liability used for payment, 2 - source import transaction, 3 - investment management fund, 4 - tax, 5 - user account, 6-rezerve for later use, 7 - cannot be removed  
        [Required, Column("categroles"), Display(Name = "Roles")] public byte[] CategoryRoles { set; get; } = new byte[1];
        [Required, Column("idcashflow"), Display(Name = "Cash flow category")] public int IdCashFlow { set; get; } = 0;
    }
}
