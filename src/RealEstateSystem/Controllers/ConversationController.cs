using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using RealEstateSystem.Models;
using RealEstateSystem.Models.ViewModels;

namespace RealEstateSystem.Controllers
{
    [Authorize]
    public class ConversationController : BaseController
    {
        // GET: Conversation - tat ca cuoc tro chuyen cua toi (khach hoac chu tin)
        public ActionResult Index()
        {
            int? uid = CurrentUserId();
            if (!uid.HasValue) return RedirectToAction("Login", "Account");

            var convs = db.Conversations
                .Where(c => c.CustomerUserId == uid.Value || c.AgentUserId == uid.Value).ToList();

            var ids = convs.Select(c => c.ConversationId).ToList();
            var userIds = convs.SelectMany(c => new[] { c.CustomerUserId, c.AgentUserId }).Distinct().ToList();
            var names = db.Users.Where(u => userIds.Contains(u.UserId)).ToDictionary(u => u.UserId, u => u.FullName);

            var propIds = convs.Where(c => c.PropertyId.HasValue).Select(c => c.PropertyId.Value).Distinct().ToList();
            var titles = db.Properties.Where(p => propIds.Contains(p.PropertyId)).ToDictionary(p => p.PropertyId, p => p.Title);

            var last = db.Messages.Where(m => ids.Contains(m.ConversationId))
                .OrderByDescending(m => m.CreatedAt).ToList()
                .GroupBy(m => m.ConversationId).ToDictionary(g => g.Key, g => g.First());

            var list = convs.Select(c =>
            {
                int other = c.CustomerUserId == uid.Value ? c.AgentUserId : c.CustomerUserId;
                Message lm;
                last.TryGetValue(c.ConversationId, out lm);
                string title = null;
                if (c.PropertyId.HasValue) titles.TryGetValue(c.PropertyId.Value, out title);
                string otherName;
                names.TryGetValue(other, out otherName);
                return new ConversationListItem
                {
                    ConversationId = c.ConversationId,
                    PropertyId = c.PropertyId,
                    PropertyTitle = title ?? "Liên hệ chung",
                    OtherUserName = otherName,
                    Status = c.Status,
                    NeedsReply = c.Status == "New" && c.AgentUserId == uid.Value,
                    LastMessage = lm != null ? lm.Content : "",
                    LastMessageAt = lm != null ? lm.CreatedAt : c.CreatedAt
                };
            }).OrderByDescending(x => x.LastMessageAt).ToList();

            return View(list);
        }

        // GET: Conversation/Details/5
        public ActionResult Details(int id)
        {
            int? uid = CurrentUserId();
            if (!uid.HasValue) return RedirectToAction("Login", "Account");

            var conv = FindMine(id, uid.Value);
            if (conv == null) return HttpNotFound();

            int other = conv.CustomerUserId == uid.Value ? conv.AgentUserId : conv.CustomerUserId;
            var users = db.Users.Where(u => u.UserId == conv.CustomerUserId || u.UserId == conv.AgentUserId)
                .ToDictionary(u => u.UserId, u => u.FullName);

            string title = null;
            if (conv.PropertyId.HasValue)
            {
                var p = db.Properties.Find(conv.PropertyId.Value);
                if (p != null) title = p.Title;
            }

            var messages = db.Messages.Where(m => m.ConversationId == id)
                .OrderBy(m => m.CreatedAt).ToList()
                .Select(m => new MessageItem
                {
                    SenderName = users.ContainsKey(m.SenderUserId) ? users[m.SenderUserId] : "",
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    IsMine = m.SenderUserId == uid.Value
                }).ToList();

            return View(new ConversationDetailViewModel
            {
                ConversationId = id,
                PropertyId = conv.PropertyId,
                PropertyTitle = title ?? "Liên hệ chung",
                OtherUserName = users.ContainsKey(other) ? users[other] : "",
                Status = conv.Status,
                IsAgent = conv.AgentUserId == uid.Value,
                Messages = messages
            });
        }

        // POST: gui tin nhan trong cuoc tro chuyen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reply(int id, string content)
        {
            int? uid = CurrentUserId();
            if (!uid.HasValue) return RedirectToAction("Login", "Account");

            var conv = FindMine(id, uid.Value);
            if (conv == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Vui lòng nhập nội dung tin nhắn.";
                return RedirectToAction("Details", new { id = id });
            }

            db.Messages.Add(new Message
            {
                ConversationId = id,
                SenderUserId = uid.Value,
                Content = content.Trim(),
                CreatedAt = DateTime.Now
            });
            // Chu tin tra loi -> Replied; khach nhan lai -> mo lai New
            conv.Status = (uid.Value == conv.AgentUserId) ? "Replied" : "New";
            db.SaveChanges();

            return RedirectToAction("Details", new { id = id });
        }

        // POST: chu tin (agent) tu doi trang thai yeu cau: New <-> Replied
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetStatus(int id, string status)
        {
            int? uid = CurrentUserId();
            if (!uid.HasValue) return RedirectToAction("Login", "Account");

            var conv = db.Conversations.FirstOrDefault(c => c.ConversationId == id && c.AgentUserId == uid.Value);
            if (conv == null) return HttpNotFound();

            if (status != "New" && status != "Replied")
            {
                TempData["Error"] = "Trạng thái không hợp lệ.";
            }
            else
            {
                conv.Status = status;
                db.SaveChanges();
                TempData["Success"] = status == "Replied" ? "Đã đánh dấu là đã phản hồi." : "Đã đánh dấu là mới.";
            }
            return RedirectToAction("Details", new { id = id });
        }

        private Conversation FindMine(int id, int uid)
        {
            return db.Conversations.FirstOrDefault(c => c.ConversationId == id
                && (c.CustomerUserId == uid || c.AgentUserId == uid));
        }
    }
}