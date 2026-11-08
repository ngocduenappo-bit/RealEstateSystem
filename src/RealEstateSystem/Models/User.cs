using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Services.Description;

namespace RealEstateSystem.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [StringLength(20)]
        [Column("role")]
        public string Role { get; set; } // "admin" hoac "user"

        [Required]
        [StringLength(150)]
        [Column("full_name")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required]
        [StringLength(150)]
        [Column("email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(15)]
        [Column("phone")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required]
        [StringLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } // "active" hoac "locked"

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [NotMapped]
        public string AvatarUrl { get { return null; } }

        // ===== Navigation properties =====

        // Cac tin dang do user nay so huu (owner_user_id)
        public virtual ICollection<Property> OwnedProperties { get; set; }

        // Cac cuoc tro chuyen ma user nay la nguoi hoi (customer_user_id)
        public virtual ICollection<Conversation> ConversationsAsCustomer { get; set; }

        // Cac cuoc tro chuyen ma user nay la nguoi duoc hoi (agent_user_id)
        public virtual ICollection<Conversation> ConversationsAsAgent { get; set; }

        // Cac tin nhan do user nay gui (sender_user_id)
        public virtual ICollection<Message> SentMessages { get; set; }

        public User()
        {
            OwnedProperties = new HashSet<Property>();
            ConversationsAsCustomer = new HashSet<Conversation>();
            ConversationsAsAgent = new HashSet<Conversation>();
            SentMessages = new HashSet<Message>();
        }
    }
}