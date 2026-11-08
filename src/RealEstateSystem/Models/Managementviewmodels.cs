using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace RealEstateSystem.Models.ViewModels
{
    // ===== Dang tin =====
    public class ListingFormViewModel
    {
        public int? PropertyId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề"), StringLength(200)]
        public string Title { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn loại bất động sản")]
        public int? PropertyTypeId { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn hình thức giao dịch")]
        public int? TransactionTypeId { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành")]
        public int? ProvinceId { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn quận/huyện")]
        public int? DistrictId { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Range(typeof(decimal), "1", "1000000000000", ErrorMessage = "Giá không hợp lệ")]
        public decimal? Price { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập diện tích")]
        [Range(typeof(decimal), "1", "1000000", ErrorMessage = "Diện tích không hợp lệ")]
        public decimal? Area { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn giấy tờ pháp lý")]
        public int? LegalStatusId { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn tình trạng nội thất")]
        public int? FurnitureStatusId { get; set; }
        [Range(0, 50)] public int? Bedrooms { get; set; }
        [Range(0, 50)] public int? Bathrooms { get; set; }
        [Range(0, 100)] public int? Floors { get; set; }
        public string Description { get; set; }
        public int[] AmenityIds { get; set; }

        public List<PropertyImage> ExistingImages { get; set; }
        public IEnumerable<SelectListItem> PropertyTypeList { get; set; }
        public IEnumerable<SelectListItem> TransactionTypeList { get; set; }
        public IEnumerable<SelectListItem> ProvinceList { get; set; }
        public IEnumerable<SelectListItem> DistrictList { get; set; }
        public IEnumerable<SelectListItem> LegalStatusList { get; set; }
        public IEnumerable<SelectListItem> FurnitureStatusList { get; set; }
        public List<Amenity> AmenityList { get; set; }
        public int? AssignedUserId { get; set; }
        public bool IsAdmin { get; set; }
        public IEnumerable<SelectListItem> UserList { get; set; }
    }

    // ===== Chat =====
    public class ConversationListItem
    {
        public int ConversationId { get; set; }
        public int? PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public string OtherUserName { get; set; }
        public string Status { get; set; }
        public bool NeedsReply { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageAt { get; set; }
    }

    public class MessageItem
    {
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMine { get; set; }
    }

    public class ConversationDetailViewModel
    {
        public int ConversationId { get; set; }
        public int? PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public string OtherUserName { get; set; }
        public string Status { get; set; }
        public bool IsAgent { get; set; }
        public List<MessageItem> Messages { get; set; }
        public string NewMessage { get; set; }
    }

    // ===== Admin =====
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProperties { get; set; }
        public int PendingCount { get; set; }
        public int ActiveCount { get; set; }
        public int SoldCount { get; set; }
        public int NewConversations { get; set; }
        public List<Property> PendingList { get; set; }
    }

    public class AdminPropertyListViewModel
    {
        public string Status { get; set; }
        public string Keyword { get; set; }
        public List<Property> Results { get; set; }
        public PagingInfo Paging { get; set; }
    }

    public class AdminUserRow
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ListingCount { get; set; }
        public int ConversationCount { get; set; }
        public bool IsAgent { get { return ListingCount > 0; } }
        public bool IsCustomer { get { return ConversationCount > 0; } }
    }

    public class AdminUserListViewModel
    {
        public string Keyword { get; set; }
        public string Posted { get; set; }   // "" = tất cả, "yes" = đã đăng tin, "no" = chưa đăng tin
        public List<AdminUserRow> Users { get; set; }
        public PagingInfo Paging { get; set; }
    }

    public class AdminUserCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên"), ValidFullName]
        public string FullName { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập email"), StringLength(150)]
        [RegularExpression(@"^[A-Za-z0-9](?:[A-Za-z0-9._+-]*[A-Za-z0-9])?@(?i:gmail\.com)$",
    ErrorMessage = "Email phải có dạng ten@gmail.com")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^\+?\d{9,14}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class AdminUserEditViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập họ tên"), ValidFullName]
        public string FullName { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^\+?\d{9,14}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string NewPassword { get; set; }   // de trong = giu mat khau cu
    }

    // ===== Danh muc =====
    public class CategoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public string ParentName { get; set; }
        public int UsageCount { get; set; }
    }

    public class CategoryListViewModel
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public int? ProvinceId { get; set; }
        public List<CategoryItem> Items { get; set; }
        public IEnumerable<SelectListItem> ProvinceList { get; set; }
        public Dictionary<string, string> Tabs { get; set; }
    }

    // ===== Admin: yeu cau cua khach (toan he thong) =====
    public class AdminInquiryRow
    {
        public int ConversationId { get; set; }
        public int? PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public string CustomerName { get; set; }
        public string AgentName { get; set; }
        public string Status { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int MessageCount { get; set; }
    }

    public class AdminInquiryListViewModel
    {
        public string Status { get; set; }
        public int? AgentId { get; set; }
        public string Keyword { get; set; }
        public int NewCount { get; set; }
        public int RepliedCount { get; set; }
        public List<AdminInquiryRow> Rows { get; set; }
        public IEnumerable<SelectListItem> AgentList { get; set; }
        public PagingInfo Paging { get; set; }
        public int ZaloCount { get; set; }
    }
    // ===== Admin: luot bam Zalo =====
    public class AdminZaloRow
    {
        public int ZaloContactId { get; set; }
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public string Location { get; set; }
        public string AgentName { get; set; }
        public DateTime ClickedAt { get; set; }
    }

    public class AdminZaloListViewModel
    {
        public string Keyword { get; set; }
        public int? AgentId { get; set; }

        // So dem tren cac tab
        public int NewCount { get; set; }
        public int RepliedCount { get; set; }
        public int ZaloCount { get; set; }

        // Thong ke (toan he thong, khong phu thuoc bo loc)
        public int TotalClicks { get; set; }
        public int TodayClicks { get; set; }
        public int Last7DaysClicks { get; set; }
        public int PropertiesWithClicks { get; set; }

        public List<AdminZaloRow> Rows { get; set; }
        public IEnumerable<SelectListItem> AgentList { get; set; }
        public PagingInfo Paging { get; set; }
    }

    public class AdminMessageItem
    {
        public string SenderName { get; set; }
        public string SenderKind { get; set; }   // "customer", "agent" hoac "admin"
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminInquiryDetailViewModel
    {
        public int ConversationId { get; set; }
        public int? PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string AgentName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AdminMessageItem> Messages { get; set; }
    }

    // ===== Admin: thong ke =====
    public class StatItem
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public int Value2 { get; set; }   // gia tri thu hai (vd: luot xem cua khu vuc)
    }

    public class TopViewedItem
    {
        public int PropertyId { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public int Views { get; set; }
        public int Inquiries { get; set; }
    }

    public class AdminStatsViewModel
    {
        public int TotalProperties { get; set; }
        public int ActiveProperties { get; set; }
        public int TotalViews { get; set; }
        public int TotalInquiries { get; set; }
        public int NewInquiries { get; set; }

        public List<StatItem> ByPropertyType { get; set; }
        public List<StatItem> ByTransactionType { get; set; }
        public List<StatItem> ByStatus { get; set; }
        public List<StatItem> InquiriesByMonth { get; set; }
        public List<StatItem> InquiriesByStatus { get; set; }
        public List<StatItem> TopLocations { get; set; }
        public List<TopViewedItem> TopViewed { get; set; }
    }
}
