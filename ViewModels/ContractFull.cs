using MyFinances.Models;

namespace MyFinances.ViewModels
{
    public class ContractFull
    {
        public FinContract FinContract { get; set; } = default!;
        public Deposit Deposit { get; set; } = default!;
        public Loan Loan { get; set; } = default!;
        public CreditCard CreditCard { get; set; } = default!;
        public Employment Employment { get; set; } = default!;
        public Income Income { get; set; } = default!;
        public Bill Bill { get; set; } = default!;
        public InvestFund InvestFund { get; set; } = default!;
    }
}
