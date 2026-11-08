using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using RealEstateSystem.Models;

namespace RealEstateSystem.Controllers
{
    public class AccountController : Controller
    {
        private RealEstateDbContext db = new RealEstateDbContext();

        // GET: Account/Register
        // Dung chung cho 2 truong hop: khach tu dang ky (anonymous),
        // hoac admin dang dang nhap tao tai khoan ho nguoi khac.
        public ActionResult Register()
        {
            bool isAdminCreating = User.Identity.IsAuthenticated && User.IsInRole("admin");
            ViewBag.IsAdminCreating = isAdminCreating;
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            bool isAdminCreating = User.Identity.IsAuthenticated && User.IsInRole("admin");
            ViewBag.IsAdminCreating = isAdminCreating;

            // Kiểm tra trùng email / số điện thoại trước, để mọi lỗi hiện cùng lúc
            bool emailExists = db.Users.Any(u => u.Email == model.Email);
            if (emailExists)
                ModelState.AddModelError("Email", "Email này đã được đăng ký.");

            string phone = TextNormalizer.Phone(model.Phone);
            if (phone.Length > 0 && db.Users.Any(u => u.Phone == phone))
                ModelState.AddModelError("Phone", "Số điện thoại này đã được đăng ký.");

            if (!ModelState.IsValid)
                return View(model);

            // Moi tai khoan tao ra, du la tu dang ky hay do admin tao,
            // deu mang role "user". Ai dang tin thi duoc coi la moi gioi.
            var user = new User
            {
                FullName = TextNormalizer.Name(model.FullName),
                Email = model.Email,
                Phone = phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "user",
                Status = "active",
                CreatedAt = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();

            if (isAdminCreating)
            {
                // Admin tao tai khoan ho nguoi khac: KHONG dang nhap thay,
                // giu nguyen phien dang nhap hien tai cua admin.
                TempData["Success"] = "Đã tạo tài khoản cho " + user.FullName + " thành công.";
                return RedirectToAction("Register");
            }

            // Truong hop tu dang ky: dang nhap luon cho khach
            SignIn(user, false);
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Login
        public ActionResult Login(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl))
            {
                string r = returnUrl.ToLower();

                if (r.Contains("/listing"))
                {
                    TempData["Error"] = "Bạn cần đăng nhập để đăng tin.";
                }
                else if (r.Contains("/inquiry"))
                {
                    TempData["Error"] = "Bạn cần đăng nhập để gửi yêu cầu liên hệ.";

                    // Gửi yêu cầu là POST nên không quay lại được địa chỉ đó,
                    // đưa người dùng về lại trang tin đang xem
                    returnUrl = (Request.UrlReferrer != null && Url.IsLocalUrl(Request.UrlReferrer.PathAndQuery))
                        ? Request.UrlReferrer.PathAndQuery
                        : Url.Action("Index", "Property");
                }
                else if (r.Contains("/property/saved"))
                {
                    TempData["Error"] = "Bạn cần đăng nhập để xem tin đã lưu.";
                }
            }
            if (Request.QueryString["reason"] == "save")
                TempData["Error"] = "Bạn cần đăng nhập để lưu tin yêu thích.";

            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Username trong view thuc chat la email, vi bang users chi co cot email
            var user = db.Users.FirstOrDefault(u => u.Email == model.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (user.Status == "locked")
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            SignIn(user, model.RememberMe);
            SignIn(user, model.RememberMe);

            // Admin đăng nhập xong thì vào thẳng trang quản trị
            if (user.Role == "admin")
                return RedirectToAction("Index", "Admin");

            if (Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            if (Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ===== Ho so ca nhan =====

        // GET: Account/Profile
        [Authorize]
        public ActionResult Profile()
        {
            var u = CurrentUser();
            if (u == null) return RedirectToAction("Login");

            return View(new ProfileViewModel
            {
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                FullName = u.FullName,
                Phone = u.Phone
            });
        }

        // POST: Account/Profile - chi cho sua ho ten va so dien thoai
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileViewModel model)
        {
            var u = CurrentUser();
            if (u == null) return RedirectToAction("Login");

            // Email, vai tro, ngay tao khong nhan tu form: lay lai tu DB de hien thi
            model.Email = u.Email;
            model.Role = u.Role;
            model.CreatedAt = u.CreatedAt;

            // Số điện thoại không được trùng tài khoản khác (bỏ qua chính mình)
            string phone = TextNormalizer.Phone(model.Phone);
            if (phone.Length > 0 && db.Users.Any(x => x.Phone == phone && x.UserId != u.UserId))
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng bởi tài khoản khác.");

            if (!ModelState.IsValid)
                return View(model);

            u.FullName = TextNormalizer.Name(model.FullName);
            u.Phone = phone;
            db.SaveChanges();

            Session["FullName"] = u.FullName;
            TempData["Success"] = "Đã cập nhật hồ sơ.";
            return RedirectToAction("Profile");
        }

        // GET: Account/ChangePassword
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: Account/ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            var u = CurrentUser();
            if (u == null) return RedirectToAction("Login");

            // Kiem tra mat khau hien tai truoc, de loi hien cung luc voi cac loi khac
            if (!string.IsNullOrEmpty(model.CurrentPassword)
                && !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, u.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
            }
            else if (!string.IsNullOrEmpty(model.NewPassword)
                && model.CurrentPassword == model.NewPassword)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải khác mật khẩu hiện tại.");
            }

            if (!ModelState.IsValid)
                return View(model);

            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            db.SaveChanges();

            TempData["Success"] = "Đã đổi mật khẩu thành công.";
            return RedirectToAction("ChangePassword");
        }

        // Nguoi dung dang dang nhap (tim theo email luu trong cookie)
        private User CurrentUser()
        {
            string email = User.Identity.Name;
            return db.Users.FirstOrDefault(x => x.Email == email);
        }

        public ActionResult AccessDenied()
        {
            return View();
        }

        // Tao cookie xac thuc, luu Role vao UserData de [Authorize(Roles = "...")] hoat dong dung
        private void SignIn(User user, bool rememberMe)
        {
            int expireMinutes = rememberMe ? 43200 : 60; // 30 ngay hoac 1 gio

            var ticket = new FormsAuthenticationTicket(
                1,
                user.Email,
                DateTime.Now,
                DateTime.Now.AddMinutes(expireMinutes),
                rememberMe,
                user.Role,
                FormsAuthentication.FormsCookiePath);

            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
            {
                HttpOnly = true
            };
            if (rememberMe)
                cookie.Expires = DateTime.Now.AddMinutes(expireMinutes);

            Response.Cookies.Add(cookie);

            // Luu them vao Session de cac Controller khac lay nhanh UserId hien tai
            Session["UserId"] = user.UserId;
            Session["FullName"] = user.FullName;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}