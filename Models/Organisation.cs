using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("finorganisation")]
    public class Organisation
    {
        [Key, Column("idfinorganisation")]
        public int IdOrg { set; get; }
        [Required, Column("idcategory"), Display(Name = "Category ID")]
        public short IdCateg { set; get; } = 0;
        [Required, Column("nameorganisation"), Display(Name = "Name organisation"), MaxLength(125)]
        public string OrganisationName { get; set; } = "Not defined";
        [Column("organisationdetails"), MaxLength(255)]
        public string? OrganisationDetails { get; set; } = "No info";
        [Required, Column("numbercontracts"), Range(0, 255)]
        public short NumberContracts { set; get; } = 0;
    }
}
