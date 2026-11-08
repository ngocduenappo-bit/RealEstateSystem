using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("transaction_types")]
    public class TransactionType
    {
        [Key]
        [Column("transaction_type_id")]
        public int TransactionTypeId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("name")]
        [Display(Name = "Loại giao dịch")]
        public string Name { get; set; } // "For Sale" hoac "For Rent"

        public virtual ICollection<Property> Properties { get; set; }

        public TransactionType()
        {
            Properties = new HashSet<Property>();
        }
    }
}