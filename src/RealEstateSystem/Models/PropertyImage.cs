using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("property_images")]
    public class PropertyImage
    {
        [Key]
        [Column("property_image_id")]
        public int PropertyImageId { get; set; }

        [Required]
        [Column("property_id")]
        public int PropertyId { get; set; }

        [Required]
        [StringLength(500)]
        [Column("image_url")]
        public string ImageUrl { get; set; }

        [Column("is_primary")]
        public bool IsPrimary { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }
    }
}