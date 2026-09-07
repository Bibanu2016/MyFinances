using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("cashflow")]
    public class CashFlowCateg
    {
        [Key, Column("idcashflow")] public required int IdCFC { get; set; } = 0;
        [Required, Column("categoryname"), DisplayName("Category name"), MaxLength(45)] public string Name { get; set; } = "Not defined";
    }
}
