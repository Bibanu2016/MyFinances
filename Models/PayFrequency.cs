using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("frequency")]
    public class PayFrequency
    {
        [Key, Column("idfrequency")]
        public short Id { get; set; }
        [Required, Column("frequencyname"), Display(Name = "Payment frequency")]
        public string Name { get; set; } = string.Empty;
        [Required, Column("frequencyfactor")] public decimal MonthlyFactor { get; set; } = 0;
        [Required, Column("frequencyinterestx")] public decimal InterestFactor { get; set; } = 0;
    }
}
