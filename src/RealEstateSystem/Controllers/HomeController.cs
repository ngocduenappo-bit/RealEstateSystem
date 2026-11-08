using RealEstateSystem.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace RealEstateSystem.Controllers
{
    public class HomeController : BaseController
    {

        // GET: Home/Index
        public ActionResult Index()
        {
            var active = db.Properties.Where(p => p.Status == "Active");

            var typeSummaries = active
                .GroupBy(p => new { p.PropertyTypeId, p.PropertyType.Name })
                .Select(g => new SummaryItem { Id = g.Key.PropertyTypeId, Name = g.Key.Name, Count = g.Count() })
                .OrderBy(x => x.Name).ToList();

            var vm = new HomeViewModel
            {
                FeaturedProperties = active
                    .Include(p => p.Images).Include(p => p.District.Province)
                    .Include(p => p.TransactionType)
                    .OrderByDescending(p => p.FeaturedUntil != null && p.FeaturedUntil > DateTime.Now)
                    .ThenByDescending(p => p.ViewCount)
                    .Take(5).ToList(),

                LatestProperties = active
                    .Include(p => p.Images).Include(p => p.District.Province)
                    .Include(p => p.TransactionType)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(6).ToList(),

                TransactionSummaries = active
                    .GroupBy(p => new { p.TransactionTypeId, p.TransactionType.Name })
                    .Select(g => new SummaryItem { Id = g.Key.TransactionTypeId, Name = g.Key.Name, Count = g.Count() })
                    .ToList(),

                PropertyTypeSummaries = typeSummaries,

                CitySummaries = active
                    .GroupBy(p => p.District.Province.Name)
                    .Select(g => new CitySummary { City = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).ToList(),

                PropertyTypeList = typeSummaries
                    .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
            };

            return View(vm);
        }

        // GET: Home/About
        public ActionResult About()
        {
            return View();
        }

        // GET: Home/Contact
        public ActionResult Contact()
        {
            return View();
        }

        // POST: Home/Contact - gui lien he chung, khong gan tin dang nao
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Contact(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                ModelState.AddModelError("", "Vui lòng nhập nội dung liên hệ.");
                return View();
            }

            int? currentUserId = CurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lien he chung chua co "agent" cu the, tam gan cho admin dau tien
            var defaultAdmin = db.Users.FirstOrDefault(u => u.Role == "admin");
            if (defaultAdmin == null)
            {
                ModelState.AddModelError("", "Hệ thống chưa có quản trị viên để tiếp nhận liên hệ.");
                return View();
            }

            if (defaultAdmin.UserId == currentUserId.Value)
            {
                ModelState.AddModelError("", "Bạn là quản trị viên tiếp nhận liên hệ nên không thể tự gửi liên hệ cho mình.");
                return View();
            }

            var conversation = new Conversation
            {
                PropertyId = null,
                CustomerUserId = currentUserId.Value,
                AgentUserId = defaultAdmin.UserId,
                Status = "New",
                CreatedAt = DateTime.Now
            };
            db.Conversations.Add(conversation);
            db.SaveChanges();

            var firstMessage = new Message
            {
                ConversationId = conversation.ConversationId,
                SenderUserId = currentUserId.Value,
                Content = message.Trim(),
                CreatedAt = DateTime.Now
            };
            db.Messages.Add(firstMessage);
            db.SaveChanges();

            TempData["Success"] = "Đã gửi liên hệ thành công. Chúng tôi sẽ phản hồi sớm nhất.";
            return RedirectToAction("Contact");
        }

    }
}