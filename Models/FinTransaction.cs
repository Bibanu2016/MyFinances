using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("fintransaction")]
    public class FinTransaction
    {
        [Key, Column("idtransaction")] public required long IdTransaction { set; get; }
        [Required, Column("transactiondate"), Display(Name = "Date")] public DateTime TransactionDate { set; get; } = DateTime.MinValue;
        [Required, Column("debit"), Display(Name = "Debit"), Range(0, 9999999, ErrorMessage = "A value between {1} and {2} is required")] public int DebitAccount { set; get; } = 0;
        [Required, Column("credit"), Display(Name = "Credit"), Range(0, 9999999, ErrorMessage = "A value between {1} and {2} is required")] public int CreditAccount { set; get; } = 0;
        [Required, Column("transactionvalue"), Display(Name = "Amount")] public decimal TransactionValue { set; get; } = decimal.Zero;
        [Required, Column("transactiondetail"), Display(Name = "Details"), MinLength(3, ErrorMessage = "A minimum of 3 char is required"), MaxLength(64, ErrorMessage = "A maximum length of 64 char is allowed")] public string TransactionDetail { set; get; } = string.Empty;
        [Required, Column("contractidentifier"), Display(Name = "ID Contract"), Range(0, 9999999, ErrorMessage = "A value between {1} and {2} is required")] public int ContractIdentifier { set; get; } = 0;
        [Required, Column("idsource"), Display(Name = "Source")] public int IdSource { set; get; } = 0;

    }
}
