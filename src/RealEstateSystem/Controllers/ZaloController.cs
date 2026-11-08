using System;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using RealEstateSystem.Models;

namespace RealEstateSystem.Controllers
{
    public class ZaloController : BaseController
    {
        private const string NotRegisteredMsg = "Số điện thoại này không đăng ký tài khoản Zalo.";

        // 0901234567 -> 84901234567; trả về null nếu không phải số di động VN hợp lệ
        private static string ToZaloPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            string digits = Regex.Replace(phone, @"\D", "");
            if (digits.StartsWith("0")) digits = "84" + digits.Substring(1);
            return Regex.IsMatch(digits, @"^84[35789]\d{8}$") ? digits : null;
        }

        // Chỗ gắn Zalo API thật sau này (hiện chỉ kiểm tra định dạng)
        private bool IsZaloRegistered(string zaloPhone)
        {
            return zaloPhone != null;
        }

        private string GetAgentZaloPhone(int propertyId, out bool notFound)
        {
            var property = db.Properties.Include(p => p.Owner)
                                        .FirstOrDefault(p => p.PropertyId == propertyId);
            notFound = property == null;
            if (property == null) return null;
            string phone = ToZaloPhone(property.Owner != null ? property.Owner.Phone : null);
            return IsZaloRegistered(phone) ? phone : null;
        }

        // GET: Zalo/Check/5 - AJAX kiểm tra trước khi chuyển sang Zalo
        [HttpGet]
        public ActionResult Check(int id)
        {
            bool notFound;
            string phone = GetAgentZaloPhone(id, out notFound);
            if (notFound) return HttpNotFound();

            if (phone == null)
                return Json(new { ok = false, message = NotRegisteredMsg }, JsonRequestBehavior.AllowGet);

            return Json(new { ok = true, url = Url.Action("Contact", "Zalo", new { id = id }) },
                        JsonRequestBehavior.AllowGet);
        }

        // GET: Zalo/Contact/5 - ghi log lượt bấm rồi chuyển sang Zalo
        public ActionResult Contact(int id)
        {
            bool notFound;
            string phone = GetAgentZaloPhone(id, out notFound);
            if (notFound) return HttpNotFound();

            if (phone == null)
            {
                TempData["Error"] = NotRegisteredMsg;
                return RedirectToAction("Details", "Property", new { id = id });
            }

            db.Set<ZaloContact>().Add(new ZaloContact { PropertyId = id, ClickedAt = DateTime.Now });
            db.SaveChanges();

            return Redirect("https://zalo.me/" + phone);
        }
    }
}