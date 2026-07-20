using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFinances.Models
{
    [Table("tangibleasset")]
    public class TangibleAsset
    {
        [Key, Column("idtangibleasset"), Display(Name = "ID")]
        public required Guid IdAsset { get; set; }
        [Required, Column("idfinaccount"), Display(Name = "Account")]
        public int IdFinAccount { get; set; } = 0;
        [Required, Column("assetname"), Display(Name = "Name"), MaxLength(45)]
        public string Name { get; set; } = "Asset name";
        [Required, Column("purchaseprice"), Display(Name = "Purchase price")]
        public int PurchasePrice { get; set; } = 0;
        [Required, Column("purchasedate"), Display(Name = "Purchase date"), DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.MinValue;
        [Required, Column("assetdetails"), Display(Name = "Details"), MaxLength(255)]
        public string AssetDetails { get; set; } = "Not provided";
        [Required, Column("depreciation"), Display(Name = "Depreciation rate")]
        public short DepreciationLife { get; set; } = 0;
        [Required, Column("evaluationdate"), Display(Name = "Evaluation date"), DataType(DataType.Date)]
        public DateTime EvaluationDate { get; set; } = DateTime.MinValue;
        [Required, Column("evaluatedvalue"), Display(Name = "Evaluation price")]
        public int EvaluationValue { get; set; } = 0;
        [Required, Column("assetsolddate"), Display(Name = "Sold date"), DataType(DataType.Date)]
        public DateTime SoldDate { get; set; } = DateTime.MaxValue;
        [Required, Column("assetsoldprice"), Display(Name = "Sold price")]
        public int SoldPrice { get; set; } = 0;
        [Required, Column("assetactive"), Display(Name = "Asset is active")]
        public bool AssetActive { get; set; } = false;
    }
}
