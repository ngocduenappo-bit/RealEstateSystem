using System;
using System.Data.Entity;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using RealEstateSystem.Models;

namespace RealEstateSystem
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Database da tao san bang script SQL: khong de EF tu tao/so sanh cau truc bang
            Database.SetInitializer<RealEstateDbContext>(null);

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // Doc Role tu cookie Forms Authentication (UserData cua ticket) de
        // User.IsInRole("admin") va [Authorize(Roles = "admin")] hoat dong dung
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var user = HttpContext.Current.User;
            if (user == null || !user.Identity.IsAuthenticated) return;

            var identity = user.Identity as FormsIdentity;
            if (identity == null) return;

            string userData = identity.Ticket.UserData;
            string[] roles = string.IsNullOrEmpty(userData)
                ? new string[0]
                : userData.Split(',');

            HttpContext.Current.User = new GenericPrincipal(identity, roles);
        }
    }
}