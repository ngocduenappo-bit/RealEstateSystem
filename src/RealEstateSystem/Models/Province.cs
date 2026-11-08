using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("provinces")]
    public class Province
    {
        [Key]
        [Column("province_id")]
        public int ProvinceId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("name")]
        [Display(Name = "Tỉnh/Thành phố")]
        public string Name { get; set; }

        public virtual ICollection<District> Districts { get; set; }

        public Province()
        {
            Districts = new HashSet<District>();
        }
    }
}