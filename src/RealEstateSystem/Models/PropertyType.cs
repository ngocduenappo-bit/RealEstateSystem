using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("property_types")]
    public class PropertyType
    {
        [Key]
        [Column("property_type_id")]
        public int PropertyTypeId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("name")]
        [Display(Name = "Loại bất động sản")]
        public string Name { get; set; }

        public virtual ICollection<Property> Properties { get; set; }

        public PropertyType()
        {
            Properties = new HashSet<Property>();
        }
    }
}