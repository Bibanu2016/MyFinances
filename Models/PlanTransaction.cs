using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("plantransaction")]
    public class PlanTransaction
    {
        [Key, Column("idtransaction")] public required long IdTransaction { set; get; }
        [Required, Column("idfacontract"), Display(Name = "Contract")] public int ContractIdentifier { set; get; } = 0;
        [Required, Column("transactiondate"), Display(Name = "Date")] public DateTime TransactionDate { set; get; } = DateTime.MinValue;
        [Required, Column("debit"), Display(Name = "Debit")] public int DebitAccount { set; get; } = 0;
        [Required, Column("credit"), Display(Name = "Credit")] public int CreditAccount { set; get; } = 0;
        [Required, Column("transactionvalue"), Display(Name = "Amount")] public decimal TransactionValue { set; get; } = decimal.Zero;
        [Required, Column("transactiondetail"), MaxLength(64), Display(Name = "Details")] public string TransactionDetail { set; get; } = string.Empty;
        [Required, Column("isexecuted"), Display(Name = "Executed")] public bool ExecutedTransaction { set; get; } = false;
        [Required, Column("idsource"), Display(Name = "Source")] public int IdSource { set; get; } = 0;


        public override string ToString()
        {
            return base.ToString() ?? string.Empty;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContractIdentifier, TransactionDate, DebitAccount, CreditAccount, TransactionValue, IdSource);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is null || GetType() != obj.GetType()) return false;
            int result = 1;
            if (obj is PlanTransaction other)
            {
                result = ContractIdentifier == other.ContractIdentifier ? result * 1 : 0;
                result = TransactionDate == other.TransactionDate ? result * 1 : 0;
                result = DebitAccount == other.DebitAccount ? result * 1 : 0;
                result = CreditAccount == other.CreditAccount ? result * 1 : 0;
                result = TransactionValue == other.TransactionValue ? result * 1 : 0;
                result = IdSource == other.IdSource ? result * 1 : 0;
                result = ExecutedTransaction == other.ExecutedTransaction ? result * 1 : 0;
            }
            return result == 1;
        }
    }
}
