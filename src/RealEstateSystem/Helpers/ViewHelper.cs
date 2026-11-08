using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using RealEstateSystem.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RealEstateSystem.Models
{
    // Gia tri trang thai luu trong cot properties.status
    public static class PropertyStatus
    {
        public const string Available = "Active";
        public const string Pending = "Pending";
        public const string SoldRented = "Sold_Rented";
    }
}

namespace RealEstateSystem.Helpers
{
    public static class ViewHelper
    {
        public const string PlaceholderImage =
            "data:image/svg+xml;charset=UTF-8,%3Csvg xmlns='http://www.w3.org/2000/svg' width='800' height='600'%3E%3Crect width='100%25' height='100%25' fill='%23e9ecef'/%3E%3Ctext x='50%25' y='50%25' dominant-baseline='middle' text-anchor='middle' font-family='Arial' font-size='36' fill='%236c757d'%3ENo image%3C/text%3E%3C/svg%3E";

        public static string FormatPrice(decimal price)
        {
            if (price >= 1000000000m) return (price / 1000000000m).ToString("0.##") + " tỷ";
            if (price >= 1000000m) return (price / 1000000m).ToString("0.##") + " triệu";
            return price.ToString("N0") + " đ";
        }

        private static bool IsRent(TransactionType t)
        {
            if (t == null || t.Name == null) return false;
            string n = t.Name.ToLower();
            return n.Contains("thuê") || n.Contains("rent");
        }

        public static string PriceText(this Property p)
        {
            string text = FormatPrice(p.Price);
            return IsRent(p.TransactionType) ? text + "/tháng" : text;
        }

        public static string TransactionLabel(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string n = name.ToLower();
            if (n.Contains("thuê") || n.Contains("rent")) return "Cho thuê";
            if (n.Contains("bán") || n.Contains("sale")) return "Mua bán";
            return name;
        }

        public static string Label(this TransactionType t)
        {
            return t == null ? "" : TransactionLabel(t.Name);
        }

        public static string TimeAgo(this DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalMinutes < 60) return (int)span.TotalMinutes + " phút trước";
            if (span.TotalHours < 24) return (int)span.TotalHours + " giờ trước";
            if (span.TotalDays < 30) return (int)span.TotalDays + " ngày trước";
            if (span.TotalDays < 365) return (int)(span.TotalDays / 30) + " tháng trước";
            return (int)(span.TotalDays / 365) + " năm trước";
        }

        public static string PrimaryImageUrl(this Property p, UrlHelper url)
        {
            var images = p.Images ?? new List<PropertyImage>();
            var img = images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).FirstOrDefault();
            return ImageSrc(url, img);
        }

        public static string ImageSrc(this UrlHelper url, PropertyImage img)
        {
            if (img == null || string.IsNullOrEmpty(img.ImageUrl)) return PlaceholderImage;
            string s = img.ImageUrl;
            if (s.StartsWith("http") || s.StartsWith("data:")) return s;
            return url.Content(s);
        }

        // Trang thai tin dang (properties.status la string)
        public static string StatusText(this string status)
        {
            if (status == PropertyStatus.Pending) return "Chờ duyệt";
            if (status == PropertyStatus.SoldRented) return "Đã bán/cho thuê";
            return "Đang hiển thị";
        }

        public static string StatusBadge(this string status)
        {
            if (status == PropertyStatus.Pending) return "bg-warning text-dark";
            if (status == PropertyStatus.SoldRented) return "bg-secondary";
            return "bg-success";
        }

        // Trang thai hoi thoai (conversations.status: New / Replied)
        public static string ConversationStatusText(string status)
        {
            return status == "Replied" ? "Đã phản hồi" : "Mới";
        }

        public static string ConversationStatusBadge(string status)
        {
            return status == "Replied" ? "bg-success" : "bg-primary";
        }
    }
    public static class SearchHelper
    {
        public const int MaxKeywordLength = 100;
        private const int MaxTerms = 8;

        // Bỏ dấu tiếng Việt (đ -> d), chữ thường, ký tự không phải chữ/số -> khoảng trắng, gộp khoảng trắng
        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            string d = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (char c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (c == 'đ' || c == 'Đ') { sb.Append('d'); continue; }
                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        // Cắt khoảng trắng thừa, giới hạn độ dài; null nếu rỗng. Dùng để hiển thị lại trong ô tìm kiếm
        public static string CleanKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return null;
            string k = Regex.Replace(keyword.Trim(), @"\s+", " ");
            if (k.Length > MaxKeywordLength) k = k.Substring(0, MaxKeywordLength).Trim();
            return k;
        }

        // Tách từ khóa thành các từ đã chuẩn hóa (tối đa 8 từ)
        public static string[] Terms(string keyword)
        {
            string n = Normalize(keyword);
            if (n.Length == 0) return new string[0];
            return n.Split(' ').Distinct().Take(MaxTerms).ToArray();
        }

        // Đúng khi văn bản chứa ĐỦ tất cả các từ (không cần đứng liền nhau)
        public static bool Matches(string haystack, string[] terms)
        {
            if (terms.Length == 0) return true;
            string h = Normalize(haystack);
            foreach (string t in terms)
                if (!h.Contains(t)) return false;
            return true;
        }
    }
}

// Cau noi: view Home/Index.cshtml dang goi Web.Helpers.ViewHelper.TransactionLabel
namespace Web.Helpers
{
    public static class ViewHelper
    {
        public static string TransactionLabel(string name)
        {
            return RealEstateSystem.Helpers.ViewHelper.TransactionLabel(name);
        }
    }
}