using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("saved_listings")]
    public class SavedListing
    {
        [Key]
        [Column("saved_id")]
        public int SavedId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("property_id")]
        public int PropertyId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }
    }
}