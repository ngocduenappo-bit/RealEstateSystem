using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace RealEstateSystem.Models
{
    // Họ tên: chỉ chữ cái (có dấu) và khoảng trắng giữa các từ, tối thiểu 2 từ
    [AttributeUsage(AttributeTargets.Property)]
    public class ValidFullNameAttribute : ValidationAttribute
    {
        private static readonly Regex LettersOnly =
            new Regex(@"^[\p{L}\p{M}\s]+$", RegexOptions.Compiled);

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string s = value as string;
            // Rỗng / toàn khoảng trắng đã được [Required] báo lỗi
            if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;

            string name = TextNormalizer.Name(s);

            if (!LettersOnly.IsMatch(name))
                return new ValidationResult("Họ tên chỉ được chứa chữ cái và khoảng trắng (không số, không ký tự đặc biệt)");
            if (name.IndexOf(' ') < 0)
                return new ValidationResult("Vui lòng nhập đầy đủ họ và tên (ít nhất 2 từ)");
            if (name.Length > 150)
                return new ValidationResult("Họ tên tối đa 150 ký tự");

            return ValidationResult.Success;
        }
    }

    public static class TextNormalizer
    {
        // Cắt khoảng trắng đầu/cuối, gộp nhiều khoảng trắng liên tiếp thành một
        public static string Name(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return Regex.Replace(s.Trim(), @"\s+", " ");
        }

        public static string Phone(string s)
        {
            return (s ?? string.Empty).Trim();
        }
    }
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [ValidFullName]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [RegularExpression(@"^[A-Za-z0-9](?:[A-Za-z0-9._+-]*[A-Za-z0-9])?@(?i:gmail\.com)$",
    ErrorMessage = "Email phải có dạng ten@gmail.com")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (10 số, bắt đầu bằng 0)")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginViewModel
    {
        // He thong chi co cot "email" trong bang users (khong co username rieng),
        // nhung dat ten property la "Username" de khop voi view da viet san (m.Username).
        // Display Name van hien "Email" de dung voi UX, va van dang nhap bang email.
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        // De khop voi @Html.HiddenFor(m => m.ReturnUrl) trong view,
        // khong can [Required] vi co the rong
        public string ReturnUrl { get; set; }
    }

    // ===== Ho so ca nhan & doi mat khau (dung chung cho admin va nguoi dang tin) =====
    public class ProfileViewModel
    {
        // Email la dinh danh dang nhap, khong cho sua; chi hien thi
        [Display(Name = "Email")]
        public string Email { get; set; }

        public string Role { get; set; }

        public DateTime CreatedAt { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (10 số, bắt đầu bằng 0)")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmNewPassword { get; set; }
    }
}
