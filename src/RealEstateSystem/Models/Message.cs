using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateSystem.Models
{
    [Table("messages")]
    public class Message
    {
        [Key]
        [Column("message_id")]
        public int MessageId { get; set; }

        [Required]
        [Column("conversation_id")]
        public int ConversationId { get; set; }

        [Required]
        [Column("sender_user_id")]
        public int SenderUserId { get; set; }

        [Required]
        [Column("content")]
        [Display(Name = "Nội dung")]
        public string Content { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; }

        [ForeignKey("SenderUserId")]
        [InverseProperty("SentMessages")]
        public virtual User Sender { get; set; }
    }
}