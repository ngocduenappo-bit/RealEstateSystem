using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using RealEstateSystem.Models;

namespace RealEstateSystem.Controllers
{
    public abstract class BaseController : Controller
    {
        protected RealEstateDbContext db = new RealEstateDbContext();

        // Lay id nguoi dung hien tai (Session, neu khong co thi tra theo email dang dang nhap)
        protected int? CurrentUserId()
        {
            object s = Session["UserId"];
            if (s != null) return (int)s;

            if (User.Identity.IsAuthenticated)
            {
                string email = User.Identity.Name;
                var u = db.Users.FirstOrDefault(x => x.Email == email);
                if (u != null) return u.UserId;
            }
            return null;
        }

        protected void DeleteUploadedFile(string url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("~/Content/uploads/")) return;
            try
            {
                string path = Server.MapPath(url);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    // Giong [Authorize(Roles = "...")] nhung khi da dang nhap ma khong du quyen
    // thi chuyen toi trang "Khong co quyen" thay vi day ve trang dang nhap.
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(
                    new { controller = "Account", action = "AccessDenied", area = "" }));
            }
            else
            {
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}
