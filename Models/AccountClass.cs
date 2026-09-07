using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("accountclass")]
    public class AccountClass
    {
        [Key, Column("idaccountclass")] public short IdAccountClass { get; set; } = 0;
        [Required, Column("idaccounttype")] public short IdAccountType { get; set; } = 0;
        [Required, Column("accountclassname")] public string AccountClassName { get; set; } = "Not defined";

    }
}
