using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("finaccount")]
    public class FinAccount
    {
        [Key, Column("idfinaccount")] public required int IdFinAccount { get; set; } = 0;
        [Required, Column("finaccountname"), DisplayName("Account name"), MaxLength(50)] public string Name { get; set; } = "Not defined";
        [Required, Column("idaccountcategory"), DisplayName("Account category")] public short AccountCategory { set; get; } = 0;
        [Required, Column("idaccountowner"), DisplayName("Account owner(s)")] public short AccountOwner { set; get; } = 0;
        [Required, Column("islinked"), DisplayName("Is linked")] public bool IsLinked {  get; set; } = false;
    }
}
