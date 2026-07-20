using System.ComponentModel.DataAnnotations;

namespace MyFinances.ViewModels
{
    public class ContractListVM
    {
        [Key] public required Guid IdFinContract { get; set; } = Guid.Empty;
        [Required] public string ContractType { get; set; } = string.Empty;
        [Required] public string ContractName { get; set; } = string.Empty;
        [Required] public string OwnerName { get; set; } = string.Empty;
        [Required] public int EquivMonthlyRate { get; set; } = 0;
    }
}
