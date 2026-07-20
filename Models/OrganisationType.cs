using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("orgcategory")]
    public class OrganisationType
    {
        [Key, Column("idorganisationcategory"), Display(Name = "ID")]
        public short Id { set; get; }
        [Required, Column("organisationcategoryname"), Display(Name = "Category Name")]
        public string CategoryName { set; get; } = string.Empty;
        [Required, Column("orgcategoryAlter")]
        public bool CanBeAltered { get; set; } = false;
    }
}
