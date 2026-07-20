using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MyFinances.Models
{
    [Table("trdictionary")]
    public class TransacDictionary
    {
        [Key, Column("idkey")] public int IdKey { get; set; } = 0;
        [Required, Column("idsource"), Display(Name = "Source account")] public int IdSource { get; set; } = 0;
        [Required, Column("strvalue"), MaxLength(45, ErrorMessage = "Less than 45 characters are required"), MinLength(3, ErrorMessage = "Minimum 3 characters are required")] public string Name { get; set; } = string.Empty;
        [Required, Column("idtarget"), Range(0,7999999, ErrorMessage = @"A value between 0 and 7999999 is required"), Display(Name = "Target account")] public int IdTarget { get; set; } = 0;
    }
}
