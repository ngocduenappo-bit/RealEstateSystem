using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using RealEstateSystem.Models;
using RealEstateSystem.Models.ViewModels;

namespace RealEstateSystem.Controllers
{
    public class PropertyController : BaseController
    {
        private const int PageSize = 9;

        // GET: Property/Index - danh sach, loc va phan trang
        public ActionResult Index(PropertySearchViewModel model, int page = 1)
        {
            // Form tim kiem duoc gui di nhung khong nhap/chon gi -> bao loi, khong tim kiem
            bool formSubmitted = Request.QueryString.AllKeys.Any(k => k != null && k != "page");
            bool noFilter = string.IsNullOrWhiteSpace(model.Keyword)
                && !model.TransactionTypeId.HasValue
                && !model.PropertyTypeId.HasValue
                && string.IsNullOrWhiteSpace(model.City)
                && string.IsNullOrWhiteSpace(model.District)
                && !model.MinPrice.HasValue && !model.MaxPrice.HasValue
                && !model.MinArea.HasValue && !model.MaxArea.HasValue
                && !model.Bedrooms.HasValue;

            if (formSubmitted && noFilter)
            {
                TempData["Error"] = "Bạn chưa nhập thông tin tìm kiếm.";
                if (Request.UrlReferrer != null && Url.IsLocalUrl(Request.UrlReferrer.PathAndQuery))
                    return Redirect(Request.UrlReferrer.PathAndQuery);
                return RedirectToAction("Index", "Home");
            }

            var now = DateTime.Now;
            var q = db.Properties.Where(p => p.Status == "Active");

            if (!string.IsNullOrWhiteSpace(model.Keyword))
            {
                string k = model.Keyword.Trim();
                q = q.Where(p => p.Title.Contains(k) || p.District.Name.Contains(k));
            }
            if (model.TransactionTypeId.HasValue)
                q = q.Where(p => p.TransactionTypeId == model.TransactionTypeId.Value);
            if (model.PropertyTypeId.HasValue)
                q = q.Where(p => p.PropertyTypeId == model.PropertyTypeId.Value);
            if (!string.IsNullOrWhiteSpace(model.City))
                q = q.Where(p => p.District.Province.Name == model.City);
            if (!string.IsNullOrWhiteSpace(model.District))
            {
                string d = model.District.Trim();
                q = q.Where(p => p.District.Name.Contains(d));
            }
            if (model.MinPrice.HasValue) q = q.Where(p => p.Price >= model.MinPrice.Value);
            if (model.MaxPrice.HasValue) q = q.Where(p => p.Price <= model.MaxPrice.Value);
            if (model.MinArea.HasValue) q = q.Where(p => p.Area >= model.MinArea.Value);
            if (model.MaxArea.HasValue) q = q.Where(p => p.Area <= model.MaxArea.Value);
            if (model.Bedrooms.HasValue) q = q.Where(p => p.Bedrooms >= model.Bedrooms.Value);

            int total = q.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            // Tin con han "day noi bat" len truoc, sau do tin moi nhat
            model.Results = q
                .Include(p => p.Images)
                .Include(p => p.District.Province)
                .Include(p => p.TransactionType)
                .Include(p => p.PropertyType)
                .Include(p => p.Owner)
                .OrderByDescending(p => p.FeaturedUntil != null && p.FeaturedUntil > now)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            model.Paging = new PagingInfo { Page = page, PageSize = PageSize, TotalItems = total };

            model.TransactionTypeList = db.TransactionTypes.OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.TransactionTypeId.ToString(), Text = t.Name })
                .ToList();
            model.PropertyTypeList = db.PropertyTypes.OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.PropertyTypeId.ToString(), Text = t.Name })
                .ToList();
            model.CityList = db.Provinces.OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Name, Text = c.Name })
                .ToList();

            return View(model);
        }


        // GET: Property/Details/5
        public ActionResult Details(int id)
        {
            var property = db.Properties
                .Include("Owner")
                .Include("PropertyType")
                .Include("TransactionType")
                .Include("District.Province")
                .Include("LegalStatus")
                .Include("FurnitureStatus")
                .Include("Images")
                .Include("PropertyAmenities.Amenity")
                .FirstOrDefault(p => p.PropertyId == id);

            if (property == null) return HttpNotFound();

            int? uid = CurrentUserId();
            bool isOwner = uid.HasValue && uid.Value == property.OwnerUserId;
            bool isAdmin = User.IsInRole("admin");

            // Tin chua duyet (Pending) chi chu tin va admin moi xem duoc
            if (property.Status == "Pending" && !isOwner && !isAdmin)
                return HttpNotFound();

            // Tinh luot xem: bo qua chu tin/admin, moi phien (session) chi tinh 1 lan cho moi tin
            string viewedKey = "Viewed_" + id;
            if (!isOwner && !isAdmin && Session[viewedKey] == null)
            {
                Session[viewedKey] = true;
                property.ViewCount += 1;
                db.SaveChanges();
            }

            // Thong tin mac dinh cua form lien he lay tu tai khoan dang dang nhap
            var inquiry = new InquiryFormViewModel();
            User currentUser = uid.HasValue ? db.Users.Find(uid.Value) : null;
            if (currentUser != null)
            {
                inquiry.FullName = currentUser.FullName;
                inquiry.Email = currentUser.Email;
                inquiry.Phone = currentUser.Phone;
            }

            // Khoi phuc noi dung form da nhap truoc khi dang nhap / gui that bai
            var draft = Session["InquiryDraft_" + id] as InquiryFormViewModel;
            if (draft != null)
            {
                Session.Remove("InquiryDraft_" + id);

                inquiry.FullName = !string.IsNullOrWhiteSpace(draft.FullName) ? draft.FullName : inquiry.FullName;
                inquiry.Email = !string.IsNullOrWhiteSpace(draft.Email) ? draft.Email : inquiry.Email;
                inquiry.Phone = !string.IsNullOrWhiteSpace(draft.Phone) ? draft.Phone : inquiry.Phone;
                inquiry.Message = draft.Message;

                if (uid.HasValue && !TempData.ContainsKey("Error") && !TempData.ContainsKey("Success"))
                {
                    TempData["Success"] = "Nội dung yêu cầu của bạn đã được giữ lại. Hãy kiểm tra và nhấn Gửi yêu cầu.";
                }
            }

            // Loi kiem tra form lien he (do InquiryController gui sang) -> hien duoi tung o nhap
            var inquiryErrors = TempData["InquiryErrors"] as Dictionary<string, string>;
            if (inquiryErrors != null)
            {
                foreach (var e in inquiryErrors)
                    ModelState.AddModelError(e.Key, e.Value);
            }

            int typeId = property.PropertyTypeId;
            var related = db.Properties
                .Where(x => x.Status == "Active" && x.PropertyId != id && x.PropertyTypeId == typeId)
                .Include(x => x.Images).Include(x => x.District.Province)
                .Include(x => x.TransactionType)
                .OrderByDescending(x => x.CreatedAt).Take(4).ToList();

            return View(new PropertyDetailViewModel
            {
                Property = property,
                Inquiry = inquiry,
                RelatedProperties = related
            });
        }


        // Tach danh sach id dang "1,2,3" (toi da max phan tu, bo trung)
        private static List<int> ParseIds(string ids, int max)
        {
            var list = new List<int>();
            if (string.IsNullOrWhiteSpace(ids)) return list;
            foreach (var part in ids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int n;
                if (int.TryParse(part.Trim(), out n) && n > 0 && !list.Contains(n)) list.Add(n);
                if (list.Count >= max) break;
            }
            return list;
        }

        // GET: Property/Saved - tin yeu thich cua tai khoan dang dang nhap
        [Authorize]
        public ActionResult Saved()
        {
            int? uid = CurrentUserId();
            if (!uid.HasValue)
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Saved", "Property") });

            var ids = db.SavedListings
                .Where(s => s.UserId == uid.Value)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => s.PropertyId).ToList();

            var props = new List<Property>();
            if (ids.Count > 0)
            {
                props = db.Properties
                    .Where(p => ids.Contains(p.PropertyId) && p.Status != "Pending")
                    .Include(p => p.Images).Include(p => p.District.Province)
                    .Include(p => p.TransactionType).Include(p => p.PropertyType).Include(p => p.Owner)
                    .ToList()
                    .OrderBy(p => ids.IndexOf(p.PropertyId)).ToList();
            }
            return View(props);
        }

        // GET: Property/SavedIds - danh sach id tin da luu cua tai khoan hien tai (JSON)
        public ActionResult SavedIds()
        {
            int? uid = CurrentUserId();
            if (!uid.HasValue) return Json(new int[0], JsonRequestBehavior.AllowGet);

            var ids = db.SavedListings.Where(s => s.UserId == uid.Value)
                .Select(s => s.PropertyId).ToList();
            return Json(ids, JsonRequestBehavior.AllowGet);
        }

        // POST: Property/ToggleSave - bam tim: chua luu thi luu, da luu thi bo luu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleSave(int id)
        {
            int? uid = CurrentUserId();
            if (!uid.HasValue) return Json(new { ok = false, needLogin = true });

            var existing = db.SavedListings.FirstOrDefault(s => s.UserId == uid.Value && s.PropertyId == id);
            bool saved;

            if (existing != null)
            {
                db.SavedListings.Remove(existing);
                saved = false;
            }
            else
            {
                var p = db.Properties.Find(id);
                if (p == null || p.Status == "Pending") return Json(new { ok = false });

                db.SavedListings.Add(new SavedListing
                {
                    UserId = uid.Value,
                    PropertyId = id,
                    CreatedAt = DateTime.Now
                });
                saved = true;
            }
            db.SaveChanges();

            int count = db.SavedListings.Count(s => s.UserId == uid.Value);
            return Json(new { ok = true, saved = saved, count = count });
        }



        // GET: Property/Brochure/5 - trang brochure, xuat PDF bang trinh duyet
        public ActionResult Brochure(int id)
        {
            var property = db.Properties
                .Include("Owner").Include("PropertyType").Include("TransactionType")
                .Include("District.Province").Include("LegalStatus").Include("FurnitureStatus")
                .Include("Images").Include("PropertyAmenities.Amenity")
                .FirstOrDefault(p => p.PropertyId == id);
            if (property == null) return HttpNotFound();

            int? uid = CurrentUserId();
            bool isOwner = uid.HasValue && uid.Value == property.OwnerUserId;
            if (property.Status == "Pending" && !isOwner && !User.IsInRole("admin"))
                return HttpNotFound();

            return View(property);
        }
    }
}