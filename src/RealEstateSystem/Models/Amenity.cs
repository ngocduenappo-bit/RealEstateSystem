using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("amenities")]
    public class Amenity
    {
        [Key]
        [Column("amenity_id")]
        public int AmenityId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("name")]
        [Display(Name = "Tiện ích")]
        public string Name { get; set; }

        public virtual ICollection<PropertyAmenity> PropertyAmenities { get; set; }

        public Amenity()
        {
            PropertyAmenities = new HashSet<PropertyAmenity>();
        }
    }
}   