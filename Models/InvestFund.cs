using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("investfund")]
    public class InvestFund
    {
        [Key, Column("idinvestfund"), Display(Name ="ID")] public Guid IdInvestFund { get; set; }
        [Required, Column("adviserfee"), Display(Name ="Adviser Fee")] public decimal AdviserFee { get; set; } = 0;

        [Required, Column("managementfee"), Display(Name = "Management Fee")] public decimal ManagementFee { get; set; } = 0;
        [Required, Column("adminfee"), Display(Name = "Admin Fee")] public decimal AdminFee { get; set; } = 0;
        [Required, Column("accountingfee"), Display(Name = "Accounting Fee")] public decimal AccountingFee { get; set; } = 0;
        [Required, Column("idcapitalgainacc"), Display(Name ="Capital gain account")] public int IdCapitalGainAcc { get; set; } = 0;
        [Required, Column("idadviseacc"), Display(Name = "Adviser fee account")] public int IdAdviseAcc { get; set; } = 0;
        [Required, Column("idmanageacc"), Display(Name = "Managemement fee account")] public int IdManageAcc { get; set; } = 0;
        [Required, Column("idadminacc"), Display(Name = "Admin fee account")] public int IdAdminAcc { get; set; } = 0;
        [Required, Column("idaccountingacc"), Display(Name = "Accounting fee account")] public int IdAccountingAcc { get; set; } = 0;
        [Required, Column("idtaxaccount"), Display(Name = "Tax account")] public int IdTaxAccount { get; set; } = 0;
    }
}
