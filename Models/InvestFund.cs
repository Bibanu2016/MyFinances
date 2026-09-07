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
        [Required, Column("idadviseracc"), Display(Name = "Adviser fee account")] public int IdAdviseAcc { get; set; } = 0;
        [Required, Column("idmanageacc"), Display(Name = "Managemement fee account")] public int IdManageAcc { get; set; } = 0;
        [Required, Column("idadminacc"), Display(Name = "Admin fee account")] public int IdAdminAcc { get; set; } = 0;
        [Required, Column("idaccountingacc"), Display(Name = "Accounting fee account")] public int IdAccountingAcc { get; set; } = 0;
        [Required, Column("idtaxaccount"), Display(Name = "Tax account")] public int IdTaxAccount { get; set; } = 0;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is null || GetType() != obj.GetType()) return false;
            var other = (InvestFund)obj;
            int result = 1;
            result *= AdviserFee == other.AdviserFee ? 1 : 0;
            result *= ManagementFee == other.ManagementFee ? 1 : 0;
            result *= AdminFee == other.AdminFee ? 1 : 0;
            result *= AccountingFee == other.AccountingFee ? 1 : 0;
            result *= IdCapitalGainAcc == other.IdCapitalGainAcc ? 1 : 0;
            result *= IdAdviseAcc == other.IdAdviseAcc ? 1 : 0;
            result *= IdManageAcc == other.IdManageAcc ? 1 : 0;
            result *= IdAdminAcc == other.IdAdminAcc ? 1 : 0;
            result *= IdAccountingAcc == other.IdAccountingAcc ? 1 : 0;
            result *= IdTaxAccount == other.IdTaxAccount ? 1 : 0;
            return result == 1;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(AdviserFee);
            hash.Add(ManagementFee);
            hash.Add(AdminFee);
            hash.Add(AccountingFee);
            hash.Add(IdCapitalGainAcc);
            hash.Add(IdAdviseAcc);
            hash.Add(IdManageAcc);
            hash.Add(IdAdminAcc);
            hash.Add(IdAccountingAcc);
            hash.Add(IdTaxAccount);
            return hash.ToHashCode();
        }
    }
}
