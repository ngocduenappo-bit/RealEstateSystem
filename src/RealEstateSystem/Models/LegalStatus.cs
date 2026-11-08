using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("legal_statuses")]
    public class LegalStatus
    {
        [Key]
        [Column("legal_status_id")]
        public int LegalStatusId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("name")]
        [Display(Name = "Giấy tờ pháp lý")]
        public string Name { get; set; }

        public virtual ICollection<Property> Properties { get; set; }

        public LegalStatus()
        {
            Properties = new HashSet<Property>();
        }
    }
}