using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("accountimportstatus")]
    public class AccountImportStatus
    {
        [Key, Column("idaccount")] public int IdAccount { get; set; } = 0;
        [Required, Column("lastdateimported")] public DateTime LastDateImported { get; set; } = DateTime.MinValue;
    }
}
