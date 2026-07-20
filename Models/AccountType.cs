using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("accounttype")]
    public class AccountType
    {
        [Key, Column("idaccounttype")] public sbyte IdAccountType { get; set; } = 0;
        [Required, Column("accounttypename")] public string AccountTypeName { get; set; } = "Not defined";
    }
}
