using Microsoft.AspNetCore.Mvc.ModelBinding;
using MyFinances.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Xml.Linq;

namespace MyFinances.Controllers
{
    public class ContractCustomValidator(ViewModels.ContractFull contractFull, ModelStateDictionary valuePairs)
    {
        private readonly ViewModels.ContractFull _cf = contractFull;
        private readonly ModelStateDictionary _msd = valuePairs;

        public void MainContractCustomValidator() 
        {
            if (_cf != null)
            {
                // Main contract custom validation
                // StartDate less then or equal to Today
                if (_cf.FinContract.StartDate.Date > DateTime.Today.Date) { _msd.TryAddModelError(nameof(_cf.FinContract.StartDate), "Start date is required to be no later than today"); }
                // EndDate greater than or equal to Today
                if (_cf.FinContract.EndDate.Date < DateTime.Today.Date) { _msd.TryAddModelError(nameof(_cf.FinContract.EndDate), "End date is required to be later than today"); }
                else
                {
                    // EndDate - StartDate >= 3 months
                    if ((_cf.FinContract.EndDate.Date - _cf.FinContract.StartDate.Date).TotalDays < 90) { _msd.TryAddModelError(nameof(_cf.FinContract.EndDate), "End date is required to be later than 90 days from the Start Date"); }
                }
                // EndAction 2 - Terminate only
                if(_cf.FinContract.ContractType == 2 && _cf.FinContract.IdEndAction != 0) { _msd.AddModelError(nameof(_cf.FinContract.IdEndAction),"Only Terminate as end action is required for a Loan"); }
                // InvoiceDate >= Today AND < Today + Frequency
                FinanceCalculator.CalculatorDates calculatorDates = new(_cf.FinContract.IdFrequency);
                DateTime ipd = calculatorDates.IncrementCycleDate(DateTime.Today);
                if(_cf.FinContract.InvoiceDate.Date < DateTime.Today.Date) { _msd.AddModelError(nameof(_cf.FinContract.InvoiceDate), "A date greater of equal to today is required"); }
                else if(_cf.FinContract.InvoiceDate.Date > ipd.Date) { _msd.AddModelError(nameof(_cf.FinContract.InvoiceDate), $"A date not greater than {ipd:d} is required"); }
                // RestrictionON - InvoiceDate <= NextInvoiceDate
                // Netdays 1, 2, 4 = 0; 3,6,7 < 30 days, 5 < 90 days
                switch (_cf.FinContract.ContractType)
                {
                    case 1:
                        if(_cf.FinContract.InvoiceNetDays != 0) { _msd.AddModelError(nameof(_cf.FinContract.InvoiceNetDays), "A net days payment of zero is required"); }
                        break;
                        case 2:
                        if (_cf.FinContract.InvoiceNetDays != 0) { _msd.AddModelError(nameof(_cf.FinContract.InvoiceNetDays), "A net days payment of zero is required"); }
                        break;
                    case 3:
                        if (_cf.FinContract.InvoiceNetDays > 30) { _msd.AddModelError(nameof(_cf.FinContract.InvoiceNetDays), "A net days payment of less than 30 days is required"); }
                        break;
                    case 4:
                        if (_cf.FinContract.InvoiceNetDays != 0) { _msd.AddModelError(nameof(_cf.FinContract.InvoiceNetDays), "A net days payment of zero is required"); }
                        break;
                    case 5:
                        if (_cf.FinContract.InvoiceNetDays > 90) { _msd.AddModelError(nameof(_cf.FinContract.InvoiceNetDays), "A net days payment of less than 90 days is required"); }
                        break;
                    case 6:
                        if (_cf.FinContract.InvoiceNetDays > 30) { _msd.AddModelError(nameof(_cf.FinContract.InvoiceNetDays), "A net days payment of less than 30 days is required"); }
                        break;
                    case 7:
                        if (_cf.FinContract.InvoiceNetDays > 30) { _msd.AddModelError(nameof(_cf.FinContract.InvoiceNetDays), "A net days payment of less than 30 days is required"); }
                        break;
                }
                // PayAccount IF == LIABILITY => BankAccount selection
                if (_cf.FinContract.IdPayAccount.ToString().StartsWith('2') && _cf.FinContract.IdBankAccount == 0)
                {
                    _msd.AddModelError(nameof(_cf.FinContract.IdBankAccount), "A valid bank account selection is required");
                }
            }
            return;
        }

        public static void EmploymentCustomValidator(Employment employment, ModelStateDictionary ms)
        {
            if (employment != null)
            {             // tax value less than gross value
                if (employment.TaxPaidCycle >= employment.GrossPaidCycle) { ms.TryAddModelError(nameof(employment.TaxPaidCycle), "Tax paid value shall be less than Gross pay value"); }
                // super value less than gross value
                if (employment.SuperPaidCycle >= employment.GrossPaidCycle) { ms.TryAddModelError(nameof(employment.TaxPaidCycle), "Tax paid value shall be less than Gross pay value"); }
                return;
            }
            return;
        }

        public static void IncomeCustomValidator(Income income, ModelStateDictionary ms) 
        {
            if (income != null) 
            {
                // tax value less than gross value
                if (income.TaxPaidCycle >= income.GrossPerCycle) { ms.TryAddModelError(nameof(income.TaxPaidCycle), "Tax paid value shall be less than Gross pay value"); }
            }
            return ;
        }


        public static void TailorContractBindingProperties(ModelStateDictionary msd, short contractType)
        {
            //remove unwanted fields - use xml file
            var csf = from s in XDocument.Load("./Data/ContractSuplFields.xml").Descendants("supfield") select s;
            if (csf.Any())
            {
                foreach (var c in csf)
                {
                    XAttribute? xa = c.Attribute("categ");
                    if (xa != null)
                    {
                        if (xa.Value != contractType.ToString()) { msd.Remove(c.Value); }
                    }
                }
            }
            return;
        }
    }
}
