using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("furniture_statuses")]
    public class FurnitureStatus
    {
        [Key]
        [Column("furniture_status_id")]
        public int FurnitureStatusId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("name")]
        [Display(Name = "Tình trạng nội thất")]
        public string Name { get; set; }

        public virtual ICollection<Property> Properties { get; set; }

        public FurnitureStatus()
        {
            Properties = new HashSet<Property>();
        }
    }
}