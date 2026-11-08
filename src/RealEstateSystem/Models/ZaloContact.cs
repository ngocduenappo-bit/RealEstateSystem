using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("zalo_contacts")]
    public class ZaloContact
    {
        [Key]
        [Column("zalo_contact_id")]
        public int ZaloContactId { get; set; }

        [Required]
        [Column("property_id")]
        public int PropertyId { get; set; }

        [Column("clicked_at")]
        public DateTime ClickedAt { get; set; }

        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }
    }
}