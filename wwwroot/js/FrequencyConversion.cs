using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace FinanceCalculator
{
    public class FrequencyConversion
    {
        public FrequencyConversion(int idFrequency)
        {
            YtocFactor = GetCycleReductionFactor(idFrequency);
            CtomFactor = GetMonthlyAdjustment(idFrequency);
        }

        public static decimal YtocFactor { get; private set; } = 0;

        public static decimal CtomFactor { get; private set; } = 0;

        public decimal AnnualToCycleConversion(decimal yValue)
        {
            return decimal.Multiply(yValue, YtocFactor);
        }

        public int CycletoMonthConversion(decimal yValue)
        {
            return (int)Math.Round(Decimal.Multiply(yValue, CtomFactor), 0);
        }

        public decimal CycleToAnnualConversion(decimal yValue, int idfrequency) => Decimal.Multiply(yValue, GetYearlyConversionFactor(idfrequency));

        private decimal GetCycleReductionFactor(int idFrequency)
        {
            decimal factor = 0;
            var freq = from c in XDocument.Load( "..//FinanceCalculator/Data/PaymentFrequency.xml").Descendants("frequency") select c;
            if (freq != null) 
            {
                foreach (var f in freq)
                {
                    XAttribute? xa = f.Attribute("id");
                    if (xa != null) { 
                        if(xa.Value == idFrequency.ToString())
                        {
                            XElement? y2f = f.Element("y2cfactor");
                            if(y2f != null)
                            {
                                bool z = Decimal.TryParse(y2f.Value, out decimal y);
                                if (z) { factor = y; }
                            }
                            break;
                        }
                    }
                }
            }
            return factor;
        }

        private decimal GetMonthlyAdjustment(int idFrequency)
        {
            decimal factor = 0;
            var freq = from c in XDocument.Load("..//FinanceCalculator/Data/PaymentFrequency.xml").Descendants("frequency") select c;
            if (freq != null)
            {
                foreach (var f in freq)
                {
                    XAttribute? xa = f.Attribute("id");
                    if (xa != null)
                    {
                        if (xa.Value == idFrequency.ToString())
                        {
                            XElement? y2f = f.Element("c2mfactor");
                            if (y2f != null)
                            {
                                bool z = Decimal.TryParse(y2f.Value, out decimal y);
                                if (z) { factor = y; }
                            }
                            break;
                        }
                    }

                }
            }
            return factor;
        }

        private decimal GetYearlyConversionFactor(int idFrequency)
        {
            decimal factor = 0;
            var freq = from c in XDocument.Load("..//FinanceCalculator/Data/PaymentFrequency.xml").Descendants("frequency") select c;
            if (freq != null)
            {
                foreach (var f in freq)
                {
                    XAttribute? xa = f.Attribute("id");
                    if (xa != null)
                    {
                        if (xa.Value == idFrequency.ToString())
                        {
                            XElement? y2f = f.Element("c2yfactor");
                            if (y2f != null)
                            {
                                bool z = Decimal.TryParse(y2f.Value, out decimal y);
                                if (z) { factor = y; }
                            }
                            break;
                        }
                    }

                }
            }
            return factor;
        }
    }
}
