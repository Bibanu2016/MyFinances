using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("accountclass")]
    public class AccountClass
    {
        [Key, Column("idaccountclass")] public sbyte IdAccountClass { get; set; } = 0;
        [Required, Column("idaccounttype")] public sbyte IdAccountType { get; set; } = 0;
        [Required, Column("accountclassname")] public string AccountClassName { get; set; } = "Not defined";

    }
}
