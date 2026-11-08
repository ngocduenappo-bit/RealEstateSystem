using RealEstateSystem.Helpers;
using RealEstateSystem.Models;
using RealEstateSystem.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace RealEstateSystem.Controllers
{
    [RoleAuthorize(Roles = "admin")]
    public class AdminController : BaseController
    {
        private const int PageSize = 10;

        public ActionResult Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = db.Users.Count(),
                TotalProperties = db.Properties.Count(),
                PendingCount = db.Properties.Count(p => p.Status == "Pending"),
                ActiveCount = db.Properties.Count(p => p.Status == "Active"),
                SoldCount = db.Properties.Count(p => p.Status == "Sold_Rented"),
                NewConversations = db.Conversations.Count(c => c.Status == "New"),
                PendingList = db.Properties.Where(p => p.Status == "Pending")
                    .Include(p => p.Owner).Include(p => p.District.Province).Include(p => p.PropertyType)
                    .OrderBy(p => p.CreatedAt).Take(5).ToList()
            };
            return View(vm);
        }

        // GET: Admin/Properties?status=Pending&keyword=...
        public ActionResult Properties(string status, string keyword, int page = 1)
        {
            keyword = SearchHelper.CleanKeyword(keyword);

            var q = db.Properties.AsQueryable();
            if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);

            var terms = SearchHelper.Terms(keyword);
            if (terms.Length > 0)
            {
                // Lấy các trường cần tìm rồi lọc trong bộ nhớ để tìm không phân biệt dấu
                var rows = q.Select(p => new
                {
                    p.PropertyId,
                    p.Title,
                    Owner = p.Owner.FullName,
                    Type = p.PropertyType.Name,
                    Trans = p.TransactionType.Name,
                    District = p.District.Name,
                    Province = p.District.Province.Name
                }).ToList();

                var ids = rows
                    .Where(r => SearchHelper.Matches(string.Join(" ",
                        r.Title, r.Owner, r.Type, r.Trans, ViewHelper.TransactionLabel(r.Trans),
                        r.District, r.Province), terms))
                    .Select(r => r.PropertyId).ToList();

                q = q.Where(p => ids.Contains(p.PropertyId));
            }

            int total = q.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var results = q.Include(p => p.Owner).Include(p => p.District.Province)
                .Include(p => p.PropertyType).Include(p => p.TransactionType)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * PageSize).Take(PageSize).ToList();

            return View(new AdminPropertyListViewModel
            {
                Status = status,
                Keyword = keyword,
                Results = results,
                Paging = new PagingInfo { Page = page, PageSize = PageSize, TotalItems = total }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Approve(int id, string returnUrl)
        {
            var p = db.Properties.Find(id);
            if (p == null) return HttpNotFound();
            p.Status = "Active";
            db.SaveChanges();
            TempData["Success"] = "Đã duyệt tin.";
            return Back(returnUrl);
        }

        // Go tin khoi trang cong khai (tro ve Pending)
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Unpublish(int id, string returnUrl)
        {
            var p = db.Properties.Find(id);
            if (p == null) return HttpNotFound();
            p.Status = "Pending";
            db.SaveChanges();
            TempData["Success"] = "Đã gỡ tin khỏi trang công khai.";
            return Back(returnUrl);
        }

        // Day tin noi bat N ngay (days = 0 de huy)
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Feature(int id, int days, string returnUrl)
        {
            var p = db.Properties.Find(id);
            if (p == null) return HttpNotFound();
            p.FeaturedUntil = days > 0 ? (DateTime?)DateTime.Now.AddDays(days) : null;
            db.SaveChanges();
            TempData["Success"] = days > 0 ? "Đã đẩy tin nổi bật " + days + " ngày." : "Đã hủy đẩy tin nổi bật.";
            return Back(returnUrl);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string returnUrl)
        {
            var p = db.Properties.Include(x => x.Images).FirstOrDefault(x => x.PropertyId == id);
            if (p == null) return HttpNotFound();
            foreach (var img in p.Images.ToList()) DeleteUploadedFile(img.ImageUrl);
            db.Properties.Remove(p);
            db.SaveChanges();
            TempData["Success"] = "Đã xóa tin.";
            return Back(returnUrl);
        }

        // ===== Yeu cau cua khach (toan he thong) =====

        // GET: Admin/Inquiries?status=New&agentId=3&keyword=...
        public ActionResult Inquiries(string status, int? agentId, string keyword, int page = 1)
        {
            var q = db.Conversations.AsQueryable();

            if (status == "New" || status == "Replied") q = q.Where(c => c.Status == status);
            else status = null;

            if (agentId.HasValue) q = q.Where(c => c.AgentUserId == agentId.Value);

            keyword = SearchHelper.CleanKeyword(keyword);
            var terms = SearchHelper.Terms(keyword);
            if (terms.Length > 0)
            {
                var searchRows = q.Select(c => new
                {
                    c.ConversationId,
                    Customer = c.Customer.FullName,
                    Agent = c.Agent.FullName,
                    Title = c.Property.Title,
                    Type = c.Property.PropertyType.Name,
                    Trans = c.Property.TransactionType.Name,
                    District = c.Property.District.Name,
                    Province = c.Property.District.Province.Name
                }).ToList();

                var matchIds = searchRows
                    .Where(r => SearchHelper.Matches(string.Join(" ",
                        r.Customer, r.Agent, r.Title, r.Type, r.Trans, ViewHelper.TransactionLabel(r.Trans),
                        r.District, r.Province), terms))
                    .Select(r => r.ConversationId).ToList();

                q = q.Where(c => matchIds.Contains(c.ConversationId));
            }

            int total = q.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var convs = q.Include(c => c.Customer).Include(c => c.Agent).Include(c => c.Property)
                .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.CreatedAt))
                .ThenByDescending(c => c.CreatedAt)
                .Skip((page - 1) * PageSize).Take(PageSize).ToList();

            // Tin nhan cuoi va so tin nhan cua cac cuoc tren trang hien tai
            var ids = convs.Select(c => c.ConversationId).ToList();
            var byConv = db.Messages.Where(m => ids.Contains(m.ConversationId))
                .OrderByDescending(m => m.CreatedAt).ToList()
                .GroupBy(m => m.ConversationId).ToDictionary(g => g.Key, g => g.ToList());

            var rows = convs.Select(c =>
            {
                List<Message> list;
                byConv.TryGetValue(c.ConversationId, out list);
                var last = list != null && list.Count > 0 ? list[0] : null;
                return new AdminInquiryRow
                {
                    ConversationId = c.ConversationId,
                    PropertyId = c.PropertyId,
                    PropertyTitle = c.Property != null ? c.Property.Title : "Liên hệ chung",
                    CustomerName = c.Customer != null ? c.Customer.FullName : "",
                    AgentName = c.Agent != null ? c.Agent.FullName : "",
                    Status = c.Status,
                    LastMessage = last != null ? last.Content : "",
                    LastMessageAt = last != null ? last.CreatedAt : c.CreatedAt,
                    MessageCount = list != null ? list.Count : 0
                };
            }).ToList();

            var agentIds = db.Conversations.Select(c => c.AgentUserId).Distinct().ToList();
            var agentList = db.Users.Where(u => agentIds.Contains(u.UserId)).OrderBy(u => u.FullName).ToList()
                .Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = u.FullName }).ToList();

            return View(new AdminInquiryListViewModel
            {
                Status = status,
                AgentId = agentId,
                Keyword = keyword,
                NewCount = db.Conversations.Count(c => c.Status == "New"),
                RepliedCount = db.Conversations.Count(c => c.Status == "Replied"),
                Rows = rows,
                AgentList = agentList,
                Paging = new PagingInfo { Page = page, PageSize = PageSize, TotalItems = total },
                ZaloCount = db.ZaloContacts.Count(),
            });
        }

        // GET: Admin/Zalo?agentId=3&keyword=...  - danh sach luot bam nut Zalo
        public ActionResult Zalo(int? agentId, string keyword, int page = 1)
        {
            keyword = SearchHelper.CleanKeyword(keyword);

            var q = db.ZaloContacts.AsQueryable();
            if (agentId.HasValue) q = q.Where(z => z.Property.OwnerUserId == agentId.Value);

            var terms = SearchHelper.Terms(keyword);
            if (terms.Length > 0)
            {
                var searchRows = q.Select(z => new
                {
                    z.ZaloContactId,
                    z.Property.Title,
                    Agent = z.Property.Owner.FullName,
                    District = z.Property.District.Name,
                    Province = z.Property.District.Province.Name
                }).ToList();

                var matchIds = searchRows
                    .Where(r => SearchHelper.Matches(string.Join(" ", r.Title, r.Agent, r.District, r.Province), terms))
                    .Select(r => r.ZaloContactId).ToList();

                q = q.Where(z => matchIds.Contains(z.ZaloContactId));
            }

            int total = q.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var clicks = q.OrderByDescending(z => z.ClickedAt).ThenByDescending(z => z.ZaloContactId)
                .Skip((page - 1) * PageSize).Take(PageSize)
                .Select(z => new
                {
                    z.ZaloContactId,
                    z.PropertyId,
                    z.Property.Title,
                    Agent = z.Property.Owner.FullName,
                    District = z.Property.District.Name,
                    Province = z.Property.District.Province.Name,
                    z.ClickedAt
                }).ToList();

            var rows = clicks.Select(c => new AdminZaloRow
            {
                ZaloContactId = c.ZaloContactId,
                PropertyId = c.PropertyId,
                PropertyTitle = c.Title,
                Location = c.District + ", " + c.Province,
                AgentName = c.Agent,
                ClickedAt = c.ClickedAt
            }).ToList();

            var agentIds = db.ZaloContacts.Select(z => z.Property.OwnerUserId).Distinct().ToList();
            var agentList = db.Users.Where(u => agentIds.Contains(u.UserId)).OrderBy(u => u.FullName).ToList()
                .Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = u.FullName }).ToList();

            DateTime today = DateTime.Today;
            DateTime weekStart = today.AddDays(-6);

            return View(new AdminZaloListViewModel
            {
                Keyword = keyword,
                AgentId = agentId,
                NewCount = db.Conversations.Count(c => c.Status == "New"),
                RepliedCount = db.Conversations.Count(c => c.Status == "Replied"),
                ZaloCount = db.ZaloContacts.Count(),
                TotalClicks = db.ZaloContacts.Count(),
                TodayClicks = db.ZaloContacts.Count(z => z.ClickedAt >= today),
                Last7DaysClicks = db.ZaloContacts.Count(z => z.ClickedAt >= weekStart),
                PropertiesWithClicks = db.ZaloContacts.Select(z => z.PropertyId).Distinct().Count(),
                Rows = rows,
                AgentList = agentList,
                Paging = new PagingInfo { Page = page, PageSize = PageSize, TotalItems = total }
            });
        }

        // GET: Admin/InquiryDetails/5 - xem toan bo noi dung mot cuoc tro chuyen
        public ActionResult InquiryDetails(int id)
        {
            var conv = db.Conversations.Include(c => c.Customer).Include(c => c.Agent).Include(c => c.Property)
                .FirstOrDefault(c => c.ConversationId == id);
            if (conv == null) return HttpNotFound();

            var messages = db.Messages.Include(m => m.Sender)
                .Where(m => m.ConversationId == id).OrderBy(m => m.CreatedAt).ToList()
                .Select(m => new AdminMessageItem
                {
                    SenderName = m.Sender != null ? m.Sender.FullName : "",
                    SenderKind = (m.Sender != null && m.Sender.Role == "admin") ? "admin"
                               : m.SenderUserId == conv.CustomerUserId ? "customer" : "agent",
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                }).ToList();

            return View(new AdminInquiryDetailViewModel
            {
                ConversationId = id,
                PropertyId = conv.PropertyId,
                PropertyTitle = conv.Property != null ? conv.Property.Title : "Liên hệ chung",
                CustomerName = conv.Customer != null ? conv.Customer.FullName : "",
                CustomerPhone = conv.Customer != null ? conv.Customer.Phone : "",
                AgentName = conv.Agent != null ? conv.Agent.FullName : "",
                Status = conv.Status,
                CreatedAt = conv.CreatedAt,
                Messages = messages
            });
        }

        // POST: admin tra loi thay/cung agent - tin nhan ghi ro ten admin
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult InquiryReply(int id, string content)
        {
            var conv = db.Conversations.Find(id);
            if (conv == null) return HttpNotFound();

            int? uid = CurrentUserId();
            if (!uid.HasValue) return RedirectToAction("Login", "Account");

            content = (content ?? "").Trim();
            if (content.Length == 0)
            {
                TempData["Error"] = "Vui lòng nhập nội dung tin nhắn.";
                return RedirectToAction("InquiryDetails", new { id = id });
            }
            if (content.Length > 2000)
            {
                TempData["Error"] = "Tin nhắn không được quá 2000 ký tự.";
                return RedirectToAction("InquiryDetails", new { id = id });
            }

            db.Messages.Add(new Message
            {
                ConversationId = id,
                SenderUserId = uid.Value,
                Content = content,
                CreatedAt = DateTime.Now
            });
            // Admin phan hoi -> Replied (tru truong hop admin la chinh nguoi hoi)
            conv.Status = (uid.Value == conv.CustomerUserId) ? "New" : "Replied";
            db.SaveChanges();

            TempData["Success"] = "Đã gửi phản hồi.";
            return RedirectToAction("InquiryDetails", new { id = id });
        }

        // POST: doi trang thai thu cong (New / Replied)
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SetInquiryStatus(int id, string status, string returnUrl)
        {
            var conv = db.Conversations.Find(id);
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

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Inquiries");
        }

        // ===== Thong ke =====

        // GET: Admin/Statistics
        public ActionResult Statistics()
        {
            // Tin dang theo loai bat dong san / hinh thuc giao dich / trang thai
            var byType = db.Properties.GroupBy(p => p.PropertyType.Name)
                .Select(g => new { Label = g.Key, N = g.Count() })
                .OrderByDescending(x => x.N).ToList();

            var byTrans = db.Properties.GroupBy(p => p.TransactionType.Name)
                .Select(g => new { Label = g.Key, N = g.Count() })
                .OrderByDescending(x => x.N).ToList();

            var byStatus = db.Properties.GroupBy(p => p.Status)
                .Select(g => new { Label = g.Key, N = g.Count() }).ToList();

            // Yeu cau cua khach: 6 thang gan nhat (thang khong co du lieu hien 0)
            DateTime start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
            var rawMonths = db.Conversations.Where(c => c.CreatedAt >= start)
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, N = g.Count() }).ToList();

            var months = new List<StatItem>();
            for (int i = 0; i < 6; i++)
            {
                DateTime m = start.AddMonths(i);
                var hit = rawMonths.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
                months.Add(new StatItem { Label = m.ToString("MM/yyyy"), Value = hit != null ? hit.N : 0 });
            }

            int newInq = db.Conversations.Count(c => c.Status == "New");
            int repliedInq = db.Conversations.Count(c => c.Status == "Replied");

            // Khu vuc pho bien: nhieu tin dang nhat (kem tong luot xem)
            var locs = db.Properties
                .GroupBy(p => new { D = p.District.Name, P = p.District.Province.Name })
                .Select(g => new { g.Key.D, g.Key.P, N = g.Count(), V = g.Sum(x => x.ViewCount) })
                .OrderByDescending(x => x.N).ThenByDescending(x => x.V).Take(8).ToList();

            // Tin duoc xem nhieu nhat
            var top = db.Properties.OrderByDescending(p => p.ViewCount).Take(10)
                .Select(p => new
                {
                    p.PropertyId,
                    p.Title,
                    D = p.District.Name,
                    P = p.District.Province.Name,
                    p.Status,
                    p.ViewCount,
                    Inq = p.Conversations.Count()
                }).ToList();

            Func<string, string> statusText = s =>
                s == "Active" ? "Đang hiển thị" : s == "Pending" ? "Chờ duyệt" : s == "Sold_Rented" ? "Đã bán/cho thuê" : s;

            return View(new AdminStatsViewModel
            {
                TotalProperties = db.Properties.Count(),
                ActiveProperties = db.Properties.Count(p => p.Status == "Active"),
                TotalViews = db.Properties.Sum(p => (int?)p.ViewCount) ?? 0,
                TotalInquiries = newInq + repliedInq,
                NewInquiries = newInq,
                ByPropertyType = byType.Select(x => new StatItem { Label = x.Label, Value = x.N }).ToList(),
                ByTransactionType = byTrans.Select(x => new StatItem { Label = x.Label, Value = x.N }).ToList(),
                ByStatus = byStatus.Select(x => new StatItem { Label = statusText(x.Label), Value = x.N }).ToList(),
                InquiriesByMonth = months,
                InquiriesByStatus = new List<StatItem>
                {
                    new StatItem { Label = "Mới", Value = newInq },
                    new StatItem { Label = "Đã phản hồi", Value = repliedInq }
                },
                TopLocations = locs.Select(x => new StatItem { Label = x.D + ", " + x.P, Value = x.N, Value2 = x.V }).ToList(),
                TopViewed = top.Select(x => new TopViewedItem
                {
                    PropertyId = x.PropertyId,
                    Title = x.Title,
                    Location = x.D + ", " + x.P,
                    Status = statusText(x.Status),
                    Views = x.ViewCount,
                    Inquiries = x.Inq
                }).ToList()
            });
        }

        private ActionResult Back(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Properties");
        }
    }
}