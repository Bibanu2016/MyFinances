using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("contractendaction")]
    public class ContractEndAction
    {
        [Key, Column("idcontractendaction"), Range(0, 16, ErrorMessage = "Value shall be between 0 to 16")]
        public short ID { get; set; } = 0;
        [Required, Column("contractendactionname"), MaxLength(16), Display(Name = "End action")]
        public string Name { get; set; } = string.Empty;
    }
}
