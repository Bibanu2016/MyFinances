using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("userapp")]
    public class UserContract
    {
        [Key, Column("iduserapp"), Display(Name = "ID")]
        public short IdUser { get; set; }
        [Required, Column("firstname"), Display(Name = "First name"), MaxLength(45), MinLength(2)]
        public string FirstName { get; set; } = string.Empty;
        [Required, Column("lastname"), Display(Name = "First name"), MaxLength(45), MinLength(2)]
        public string LastName { get; set; } = string.Empty;
        [Required, Column("username"), Display(Name = "Username"), MaxLength(45), MinLength(6)]
        public string UserName { get; set; } = string.Empty;
        [Required, Column("userapppass"), Display(Name = "Password"), MaxLength(45), MinLength(8), DataType(DataType.Password)]
        public string UserPassword { get; set; } = string.Empty;
        [Required, Column("userappemail"), Display(Name = "Email"), DataType(DataType.EmailAddress)]
        public string UserEmail { get; set; } = string.Empty;
        [Required, Column("userappphone"), Display(Name = "Mobile phone"), DataType(DataType.PhoneNumber)]
        public string UserPhone { get; set; } = string.Empty;
        [Required, Column("userappverified"), Display(Name = "Verified user")]
        public bool UserVerified { get; set; } = false;
        [Required, Column("usertaxaccount"), Display(Name = "Tax account")]
        public int UserTaxAccount { set; get; } = 0;
        [Required, Column("groupofusers"), Display(Name = "Is Group?")]
        public bool IsUserGroup { get; set; } = false;
        [Required, Column("groupusersidcsv"), Display(Name = "Users list")]
        public string UsersListCsv { get; set; } = string.Empty;
        [Required, Column("useraccountid")]
        public int UserAccountID { get; set; } = 0;
        [Required, Column("userreceivable")]
        public int UserReceivable { get; set; } = 0;
        [Required, Column("userpayable")]
        public int UserPayable { get; set; } = 0;
        [Required, Column("usercashaccount"), DisplayName("Cash Account")]
        public int UserCash { get; set; } = 0;
        [Display(Name = "Full Name")]
        public string FullName { get { return FirstName + " " + LastName; } }
    }
}
