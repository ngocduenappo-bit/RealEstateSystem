using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using RealEstateSystem.Models;

namespace RealEstateSystem.Controllers
{
    [Authorize]
    public class InquiryController : BaseController
    {
        private const string GmailSuffix = "@gmail.com";

        // Kiem tra du lieu form lien he. Tra ve danh sach loi theo ten o nhap (rong = hop le)
        private static Dictionary<string, string> ValidateForm(string fullName, string email, string phone, string message)
        {
            var errors = new Dictionary<string, string>();

            // Ho ten: toi da 100 ky tu (tinh ca dau cach), chi gom chu cai va dau cach
            string name = (fullName ?? "").Trim();
            if (name.Length == 0)
                errors["FullName"] = "Vui lòng nhập họ và tên.";
            else if (name.Length > 100)
                errors["FullName"] = "Họ và tên không được quá 100 ký tự (tính cả dấu cách).";
            else if (!Regex.IsMatch(name, @"^[\p{L}\p{M} ]+$"))
                errors["FullName"] = "Họ và tên chỉ được nhập chữ cái, không nhập số hoặc ký tự đặc biệt.";

            // Email: phan truoc @gmail.com toi da 100 ky tu, duoi phai la @gmail.com
            string mail = (email ?? "").Trim();
            if (mail.Length == 0)
            {
                errors["Email"] = "Vui lòng nhập email.";
            }
            else if (!mail.EndsWith(GmailSuffix, StringComparison.OrdinalIgnoreCase))
            {
                errors["Email"] = "Email phải có đuôi @gmail.com.";
            }
            else
            {
                string local = mail.Substring(0, mail.Length - GmailSuffix.Length);
                if (local.Length == 0)
                    errors["Email"] = "Vui lòng nhập tên email trước @gmail.com.";
                else if (local.Length > 100)
                    errors["Email"] = "Email không được quá 100 ký tự (không tính @gmail.com).";
                else if (!Regex.IsMatch(local, @"^[A-Za-z0-9._%+\-]+$"))
                    errors["Email"] = "Email chứa ký tự không hợp lệ.";
            }

            // So dien thoai: dung 10 chu so
            string ph = (phone ?? "").Trim();
            if (ph.Length == 0)
                errors["Phone"] = "Vui lòng nhập số điện thoại.";
            else if (!Regex.IsMatch(ph, "^[0-9]+$"))
                errors["Phone"] = "Số điện thoại chỉ được nhập chữ số.";
            else if (ph.Length != 10)
                errors["Phone"] = "Số điện thoại phải gồm đúng 10 chữ số.";

            // Noi dung: toi da 500 ky tu, khong tinh khoang trang (dau cach, xuong dong...)
            int count = (message ?? "").Count(c => !char.IsWhiteSpace(c));
            if (count == 0)
                errors["Message"] = "Vui lòng nhập nội dung.";
            else if (count > 500)
                errors["Message"] = "Nội dung không được quá 500 ký tự (không tính khoảng trắng).";

            return errors;
        }

        // POST: Inquiry/Create - gui yeu cau lien he ve mot tin dang (luu thanh chat)
        // [AllowAnonymous]: cho action chay ca khi chua dang nhap de giu lai noi dung form
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int propertyId, string fullName, string email, string phone, string message)
        {
            var property = db.Properties.Find(propertyId);
            if (property == null || property.Status == "Pending") return HttpNotFound();

            string detailsUrl = Url.Action("Details", "Property", new { id = propertyId });

            // Luon giu lai noi dung form de hien lai tren trang chi tiet
            Session["InquiryDraft_" + propertyId] = new InquiryFormViewModel
            {
                FullName = fullName,
                Email = email,
                Phone = phone,
                Message = message
            };

            // Du lieu khong hop le -> bao loi duoi tung o, giu nguyen noi dung da nhap
            var errors = ValidateForm(fullName, email, phone, message);
            if (errors.Count > 0)
            {
                TempData["InquiryErrors"] = errors;
                TempData["Error"] = "Gửi yêu cầu thất bại: vui lòng kiểm tra lại thông tin đã nhập.";
                return Redirect(detailsUrl);
            }

            int? uid = User.Identity.IsAuthenticated ? CurrentUserId() : null;

            if (!uid.HasValue)
            {
                // Chua dang nhap -> sang trang dang nhap, dang nhap xong quay lai trang tin nay
                TempData["Error"] = "Bạn cần đăng nhập để gửi yêu cầu liên hệ.";
                return RedirectToAction("Login", "Account", new { returnUrl = detailsUrl });
            }

            if (property.OwnerUserId == uid.Value)
            {
                TempData["Error"] = "Gửi yêu cầu thất bại: bạn không thể nhắn tin cho tin đăng của chính mình.";
                return Redirect(detailsUrl);
            }

            try
            {
                // Nhan lai thi mo lai cuoc tro chuyen cu
                var conv = db.Conversations.FirstOrDefault(c =>
                    c.PropertyId == propertyId
                    && c.CustomerUserId == uid.Value
                    && c.AgentUserId == property.OwnerUserId);

                if (conv == null)
                {
                    conv = new Conversation
                    {
                        PropertyId = propertyId,
                        CustomerUserId = uid.Value,
                        AgentUserId = property.OwnerUserId,
                        Status = "New",
                        CreatedAt = DateTime.Now
                    };
                    db.Conversations.Add(conv);
                }
                else
                {
                    conv.Status = "New";
                }
                db.SaveChanges();

                string content = message.Trim()
                    + "\n\n— Liên hệ: " + fullName.Trim() + " | " + phone.Trim() + " | " + email.Trim().ToLower();

                db.Messages.Add(new Message
                {
                    ConversationId = conv.ConversationId,
                    SenderUserId = uid.Value,
                    Content = content,
                    CreatedAt = DateTime.Now
                });
                db.SaveChanges();
            }
            catch (Exception)
            {
                // Giu nguyen noi dung form (da luu trong Session) de nguoi dung gui lai
                TempData["Error"] = "Gửi yêu cầu thất bại. Vui lòng thử lại.";
                return Redirect(detailsUrl);
            }

            Session.Remove("InquiryDraft_" + propertyId);
            TempData["Success"] = "Yêu cầu liên hệ đã được gửi thành công.";
            return Redirect(detailsUrl);
        }
    }
}