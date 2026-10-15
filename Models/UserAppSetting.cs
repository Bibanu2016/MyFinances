using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("usersettings")]
    public class UserAppSetting
    {
        [Key, Column("idusersettings"), Display(Name = "ID")] public int IDUS { set; get; } = 0;
        [Required, Column("fystartmonth"), Display(Name = "FY Start Month")] public short FYStartMonth { set; get; } = 7;
        [Required, Column("forecastyears"), Display(Name = "Forecast years")] public short ForeYear { set; get; } = 3;
        [Required, Column("ccpayoffminbalance"), Display(Name = "CC Payoff Bal.")] public int CCPayOffBal { set; get; } = 100;
        [Required, Column("cashpurchratio"), Display(Name = "CC Cash-Purch")] public short CCCtoP { set; get; } = 10;

    }
}
