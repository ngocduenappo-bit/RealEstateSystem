using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("districts")]
    public class District
    {
        [Key]
        [Column("district_id")]
        public int DistrictId { get; set; }

        [Required]
        [Column("province_id")]
        public int ProvinceId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("name")]
        [Display(Name = "Quận/Huyện")]
        public string Name { get; set; }

        [ForeignKey("ProvinceId")]
        public virtual Province Province { get; set; }

        public virtual ICollection<Property> Properties { get; set; }

        public District()
        {
            Properties = new HashSet<Property>();
        }
        public override string ToString()
        {
            return Name;
        }
    }
}