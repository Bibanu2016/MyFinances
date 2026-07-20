using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("contracttype")]
    public class ContractType
    {
        [Key, Column("idcontracttype")] public short IdContractType { get; set; } = 0;
        [Required, Column("contracttypename"), Display(Name = "Contract type")] public string ContractTypeName { get; set; } = "NOT DEFINED";
        [Required, Column("controlcol")] public string ControlProgram { get; set; } = string.Empty;
    }
}
