using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("payabletype")]
    public class PayableType
    {
        [Key, Column("idpayabletype"), Range(0, 9, ErrorMessage = "Maximum value of 9 exceeded")] public short IdPT { get; set; } = 0;
        [Required, Column("payabletypeName"), MaxLength(45, ErrorMessage = "Maximum string length of 45 exceeded")] public string Name { get; set; } = string.Empty;
    }
}
