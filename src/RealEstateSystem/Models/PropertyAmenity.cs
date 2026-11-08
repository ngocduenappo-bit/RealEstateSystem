using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("property_amenities")]
    public class PropertyAmenity
    {
        // Khoa chinh kep (composite key): property_id + amenity_id
        [Key]
        [Column("property_id", Order = 0)]
        public int PropertyId { get; set; }

        [Key]
        [Column("amenity_id", Order = 1)]
        public int AmenityId { get; set; }

        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }

        [ForeignKey("AmenityId")]
        public virtual Amenity Amenity { get; set; }
    }
}