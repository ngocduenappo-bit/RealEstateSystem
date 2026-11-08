using System;
using System.Linq;
using System.Web.Mvc;
using RealEstateSystem.Models;
using RealEstateSystem.Models.ViewModels;
using RealEstateSystem.Helpers;

namespace RealEstateSystem.Controllers
{
    [RoleAuthorize(Roles = "admin")]
    public class AdminUserController : BaseController
    {
        private const int PageSize = 10;

        public ActionResult Index(string keyword, string posted, int page = 1)
        {
            var q = db.Users.AsQueryable();
            // Lọc theo việc đăng tin (không tính admin)
            if (posted == "yes")
                q = q.Where(u => u.Role != "admin" && db.Properties.Any(p => p.OwnerUserId == u.UserId));
            else if (posted == "no")
                q = q.Where(u => u.Role != "admin" && !db.Properties.Any(p => p.OwnerUserId == u.UserId));
            else
                posted = "";
            keyword = SearchHelper.CleanKeyword(keyword);
            var terms = SearchHelper.Terms(keyword);
            if (terms.Length > 0)
            {
                var rows0 = q.Select(u => new { u.UserId, u.FullName, u.Email, u.Phone }).ToList();
                var matchIds = rows0
                    .Where(r => SearchHelper.Matches(string.Join(" ", r.FullName, r.Email, r.Phone), terms))
                    .Select(r => r.UserId).ToList();
                q = q.Where(u => matchIds.Contains(u.UserId));
            }

            int total = q.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var users = q.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * PageSize).Take(PageSize).ToList();
            var ids = users.Select(u => u.UserId).ToList();

            var listings = db.Properties.Where(p => ids.Contains(p.OwnerUserId))
                .GroupBy(p => p.OwnerUserId).Select(g => new { Id = g.Key, C = g.Count() })
                .ToDictionary(x => x.Id, x => x.C);
            var chats = db.Conversations.Where(c => ids.Contains(c.CustomerUserId))
                .GroupBy(c => c.CustomerUserId).Select(g => new { Id = g.Key, C = g.Count() })
                .ToDictionary(x => x.Id, x => x.C);

            var rows = users.Select(u => new AdminUserRow
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                ListingCount = listings.ContainsKey(u.UserId) ? listings[u.UserId] : 0,
                ConversationCount = chats.ContainsKey(u.UserId) ? chats[u.UserId] : 0
            }).ToList();

            return View(new AdminUserListViewModel
            {
                Keyword = keyword,
                Users = rows,
                Paging = new PagingInfo { Page = page, PageSize = PageSize, TotalItems = total }
            });
            return View(new AdminUserListViewModel
            {
                Keyword = keyword,
                Posted = posted,
                Users = rows,
                Paging = new PagingInfo { Page = page, PageSize = PageSize, TotalItems = total }
            });
        }

        public ActionResult Create()
        {
            return View(new AdminUserCreateViewModel { Role = "user" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(AdminUserCreateViewModel vm)
        {
            string email = (vm.Email ?? "").Trim().ToLower();
            if (vm.Role != "admin" && vm.Role != "user") vm.Role = "user";
            if (!string.IsNullOrEmpty(email) && db.Users.Any(u => u.Email == email))
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");

            string phone = TextNormalizer.Phone(vm.Phone);
            if (phone.Length > 0 && db.Users.Any(u => u.Phone == phone))
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng.");

            if (!ModelState.IsValid) return View(vm);

            db.Users.Add(new User
            {
                Role = vm.Role,
                FullName = TextNormalizer.Name(vm.FullName),
                Email = email,
                Phone = phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password),
                Status = "active",
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();

            TempData["Success"] = "Đã tạo tài khoản.";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            var u = db.Users.Find(id);
            if (u == null) return HttpNotFound();
            return View(new AdminUserEditViewModel
            {
                UserId = u.UserId,
                Email = u.Email,
                FullName = u.FullName,
                Phone = u.Phone,
                Role = u.Role,
                Status = u.Status
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(AdminUserEditViewModel vm)
        {
            var u = db.Users.Find(vm.UserId);
            if (u == null) return HttpNotFound();
            vm.Email = u.Email;

            if (vm.Role != "admin" && vm.Role != "user") vm.Role = (u.Role == "admin") ? "admin" : "user";
            if (vm.Status != "active" && vm.Status != "locked") vm.Status = u.Status;

            // Khong cho admin tu ha quyen / tu khoa chinh minh
            if (vm.UserId == CurrentUserId() && (vm.Role != "admin" || vm.Status != "active"))
                ModelState.AddModelError("", "Bạn không thể tự hạ quyền hoặc tự khóa tài khoản của mình.");

            // Số điện thoại không được trùng tài khoản khác (bỏ qua chính user đang sửa)
            string phone = TextNormalizer.Phone(vm.Phone);
            if (phone.Length > 0 && db.Users.Any(x => x.Phone == phone && x.UserId != vm.UserId))
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng bởi tài khoản khác.");

            if (!ModelState.IsValid) return View(vm);

            u.FullName = TextNormalizer.Name(vm.FullName);
            u.Phone = phone;
            u.Role = vm.Role;
            u.Status = vm.Status;
            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
                u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
            db.SaveChanges();

            TempData["Success"] = "Đã cập nhật tài khoản.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ToggleLock(int id)
        {
            var u = db.Users.Find(id);
            if (u == null) return HttpNotFound();
            if (id == CurrentUserId())
            {
                TempData["Error"] = "Bạn không thể tự khóa tài khoản của mình.";
                return RedirectToAction("Index");
            }
            u.Status = u.Status == "locked" ? "active" : "locked";
            db.SaveChanges();
            TempData["Success"] = u.Status == "locked" ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.";
            return RedirectToAction("Index");
        }
    }
}