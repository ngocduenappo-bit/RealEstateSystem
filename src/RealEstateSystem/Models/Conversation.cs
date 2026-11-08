using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Services.Description;

namespace RealEstateSystem.Models
{
    [Table("conversations")]
    public class Conversation
    {
        [Key]
        [Column("conversation_id")]
        public int ConversationId { get; set; }

        // Co the NULL neu la lien he chung, khong gan tin dang nao
        [Column("property_id")]
        public int? PropertyId { get; set; }

        [Required]
        [Column("customer_user_id")]
        public int CustomerUserId { get; set; }

        [Required]
        [Column("agent_user_id")]
        public int AgentUserId { get; set; }

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } // "New" hoac "Replied"

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // ===== Navigation properties =====

        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }

        // Can InverseProperty vi User co 2 collection khac nhau
        // tro ve Conversation (ConversationsAsCustomer / ConversationsAsAgent)
        [ForeignKey("CustomerUserId")]
        [InverseProperty("ConversationsAsCustomer")]
        public virtual User Customer { get; set; }

        [ForeignKey("AgentUserId")]
        [InverseProperty("ConversationsAsAgent")]
        public virtual User Agent { get; set; }

        public virtual ICollection<Message> Messages { get; set; }

        public Conversation()
        {
            Messages = new HashSet<Message>();
        }
    }
}