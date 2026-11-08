using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace RealEstateSystem.Models
{
    [Table("properties")]
    public class Property
    {
        [Key]
        [Column("property_id")]
        public int PropertyId { get; set; }

        [Required]
        [Column("owner_user_id")]
        public int OwnerUserId { get; set; }

        [Required]
        [Column("property_type_id")]
        [Display(Name = "Loại bất động sản")]
        public int PropertyTypeId { get; set; }

        [Required]
        [Column("transaction_type_id")]
        [Display(Name = "Loại giao dịch")]
        public int TransactionTypeId { get; set; }

        [Required]
        [Column("district_id")]
        [Display(Name = "Quận/Huyện")]
        public int DistrictId { get; set; }

        [Required]
        [StringLength(200)]
        [Column("title")]
        [Display(Name = "Tiêu đề tin đăng")]
        public string Title { get; set; }

        [Required]
        [Column("price", TypeName = "decimal")]
        [Display(Name = "Giá")]
        public decimal Price { get; set; }

        [Required]
        [Column("area", TypeName = "decimal")]
        [Display(Name = "Diện tích (m²)")]
        public decimal Area { get; set; }

        [Required]
        [Column("legal_status_id")]
        [Display(Name = "Giấy tờ pháp lý")]
        public int LegalStatusId { get; set; }

        [Required]
        [Column("furniture_status_id")]
        [Display(Name = "Tình trạng nội thất")]
        public int FurnitureStatusId { get; set; }

        [Column("bedrooms")]
        [Display(Name = "Số phòng ngủ")]
        public int? Bedrooms { get; set; }

        [Column("bathrooms")]
        [Display(Name = "Số phòng vệ sinh")]
        public int? Bathrooms { get; set; }

        [Column("floors")]
        [Display(Name = "Số tầng")]
        public int? Floors { get; set; }

        [Column("description")]
        [Display(Name = "Mô tả chi tiết")]
        public string Description { get; set; }

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } // "Active", "Pending", "Sold_Rented"

        // NULL neu khong duoc day noi bat; co gia tri va con trong tuong lai = dang duoc day
        [Column("featured_until")]
        public DateTime? FeaturedUntil { get; set; }

        [Column("view_count")]
        public int ViewCount { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // ===== Navigation properties =====

        [ForeignKey("OwnerUserId")]
        public virtual User Owner { get; set; }

        [ForeignKey("PropertyTypeId")]
        public virtual PropertyType PropertyType { get; set; }

        [ForeignKey("TransactionTypeId")]
        public virtual TransactionType TransactionType { get; set; }

        [ForeignKey("DistrictId")]
        public virtual District District { get; set; }

        [ForeignKey("LegalStatusId")]
        public virtual LegalStatus LegalStatus { get; set; }

        [ForeignKey("FurnitureStatusId")]
        public virtual FurnitureStatus FurnitureStatus { get; set; }

        public virtual ICollection<PropertyImage> Images { get; set; }
        public virtual ICollection<PropertyAmenity> PropertyAmenities { get; set; }
        public virtual ICollection<Conversation> Conversations { get; set; }
        public virtual ICollection<ZaloContact> ZaloContacts { get; set; }
        
        [NotMapped]
        public int Id { get { return PropertyId; } }

        [NotMapped]
        public string City
        {
            get { return (District != null && District.Province != null) ? District.Province.Name : ""; }
        }

        [NotMapped]
        public bool IsFeatured
        {
            get { return FeaturedUntil != null && FeaturedUntil > DateTime.Now; }
        }

        [NotMapped]
        public User Agent { get { return Owner; } }

        // DB hiện chưa có cột địa chỉ chi tiết (số nhà, đường)
        [NotMapped]
        public string Address { get { return ""; } }

        [NotMapped]
        public string Amenities
        {
            get
            {
                if (PropertyAmenities == null) return "";
                return string.Join(", ", PropertyAmenities
                    .Where(a => a.Amenity != null).Select(a => a.Amenity.Name));
            }
        }

        public Property()
        {
            Images = new HashSet<PropertyImage>();
            PropertyAmenities = new HashSet<PropertyAmenity>();
            Conversations = new HashSet<Conversation>();
            ZaloContacts = new HashSet<ZaloContact>();
        }

        [Column("created_by_user_id")]
        public int? CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; }
    }
}