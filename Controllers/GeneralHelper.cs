namespace MyFinances.Controllers
{
    public class GeneralHelper(IConfiguration configuration)
    {
        private readonly IConfiguration _configuration = configuration;

        public DateTime StartFinancialYear(DateTime referenceDate)
        {
            //by default set the date as the last day of the current year
            string x;
            DateTime result = DateTime.Parse(referenceDate.Year.ToString() + "-01-01");
            var m = _configuration.GetSection("MySettings").GetValue<int>("MonthStartFY");
            if (m > 0 && m < 13)
            {
                if (referenceDate.Month < m)
                {
                    x = (referenceDate.Year - 1).ToString();
                }
                else
                {
                    x = referenceDate.Year.ToString();
                }
                string ms = m < 10 ? ("0" + m.ToString()) : m.ToString();
                result = DateTime.Parse(x + "-" + ms + "-01");
            }
            return result;
        }

        public DateTime EndPeriodPlanTransaction(DateTime referenceDate, DateTime endDate, short idEnd)
        {
            DateTime startFY = StartFinancialYear(referenceDate);
            int x = _configuration.GetSection("MySettings").GetValue<int>("YearsPlanning");
            int y = _configuration.GetSection("MySettings").GetValue<int>("MaxYearPlanning");
            if (y == 0) { y = 5; } // guard against a zero value for maximum planning
            if (x > y) { x = y; } // guard for errors where the normal plan term is set greater than maximum
            DateTime temp = startFY.AddYears(x + 1);
            if (idEnd == 0)
            {
                //contract terminates on the End date
                return temp >= endDate ? endDate : temp;
            }
            else
            {
                //contract renews or is prpetual - terminate on the set YearsPlanning
                return temp;
            }
        }

    }
}
