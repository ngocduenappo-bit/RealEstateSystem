using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RealEstateSystem.Models;
using RealEstateSystem.Models.ViewModels;

namespace RealEstateSystem.Controllers
{
    [Authorize]
    public class ListingController : BaseController
    {
        private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp" };
        private const int MaxImageBytes = 5 * 1024 * 1024;          // toi da moi anh
        private const long MaxTotalBytes = 20L * 1024 * 1024;       // toi da tong dung luong anh cua 1 tin

        // GET: Listing - tin cua toi
        public ActionResult Index()
        {
            int? uid = CurrentUserId();
            if (!uid.HasValue) return RedirectToAction("Login", "Account");

            var q = db.Properties.AsQueryable();
            if (User.IsInRole("admin"))
                q = q.Where(p => p.CreatedByUserId == uid.Value || p.OwnerUserId == uid.Value);
            else
                q = q.Where(p => p.OwnerUserId == uid.Value);

            var list = q.Include(p => p.Images).Include(p => p.District.Province)
                .Include(p => p.TransactionType).Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt).ToList();
            return View(list);
        }

        public ActionResult Create()
        {
            var vm = new ListingFormViewModel();
            PopulateLists(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ListingFormViewModel vm, IEnumerable<HttpPostedFileBase> images)
        {
            int? uid = CurrentUserId();
            if (!uid.HasValue) return RedirectToAction("Login", "Account");

            CheckDistrict(vm);
            CheckAssignedUser(vm, null);
            if (!ModelState.IsValid)
            {
                PopulateLists(vm);
                return View(vm);
            }

            var p = new Property
            {
                OwnerUserId = User.IsInRole("admin") ? vm.AssignedUserId.Value : uid.Value,
                CreatedByUserId = uid.Value,
                Status = "Pending",          // cho admin duyet
                ViewCount = 0,
                CreatedAt = DateTime.Now
            };

            ApplyForm(p, vm);
            db.Properties.Add(p);
            db.SaveChanges();

            SaveAmenities(p.PropertyId, vm.AmenityIds);
            int skipped = SaveImages(p.PropertyId, images);

            TempData["Success"] = "Đã đăng tin, vui lòng chờ quản trị viên duyệt.";
            if (skipped > 0) TempData["Error"] = skipped + " ảnh bị bỏ qua (chỉ nhận JPG/PNG/WEBP, tối đa 5MB mỗi ảnh và tổng 20MB mỗi tin).";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            var p = FindOwned(id);
            if (p == null) return HttpNotFound();

            var vm = new ListingFormViewModel
            {
                PropertyId = p.PropertyId,
                Title = p.Title,
                PropertyTypeId = p.PropertyTypeId,
                TransactionTypeId = p.TransactionTypeId,
                ProvinceId = p.District.ProvinceId,
                DistrictId = p.DistrictId,
                Price = p.Price,
                Area = p.Area,
                LegalStatusId = p.LegalStatusId,
                FurnitureStatusId = p.FurnitureStatusId,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                Floors = p.Floors,
                Description = p.Description,
                AmenityIds = p.PropertyAmenities.Select(a => a.AmenityId).ToArray(),
                AssignedUserId = p.OwnerUserId,
                ExistingImages = p.Images.OrderBy(i => i.SortOrder).ToList()
            };
            PopulateLists(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, ListingFormViewModel vm, IEnumerable<HttpPostedFileBase> images)
        {
            var p = FindOwned(id);
            if (p == null) return HttpNotFound();

            CheckDistrict(vm);
            CheckAssignedUser(vm, p.OwnerUserId);
            if (!ModelState.IsValid)
            {
                vm.PropertyId = id;
                vm.ExistingImages = p.Images.OrderBy(i => i.SortOrder).ToList();
                PopulateLists(vm);
                return View(vm);
            }

            ApplyForm(p, vm);
            if (User.IsInRole("admin")) p.OwnerUserId = vm.AssignedUserId.Value;
            db.SaveChanges();

            SaveAmenities(id, vm.AmenityIds);
            int skipped = SaveImages(id, images);

            TempData["Success"] = "Đã cập nhật tin đăng.";
            if (skipped > 0) TempData["Error"] = skipped + " ảnh bị bỏ qua (chỉ nhận JPG/PNG/WEBP, tối đa 5MB mỗi ảnh và tổng 20MB mỗi tin).";
            return RedirectToAction("Edit", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var p = FindOwned(id);
            if (p == null) return HttpNotFound();

            foreach (var img in p.Images.ToList()) DeleteUploadedFile(img.ImageUrl);
            db.Properties.Remove(p);
            db.SaveChanges();

            TempData["Success"] = "Đã xóa tin đăng.";
            return RedirectToAction("Index");
        }

        // Danh dau da ban / da cho thue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkSold(int id)
        {
            var p = FindOwned(id);
            if (p == null) return HttpNotFound();
            p.Status = "Sold_Rented";
            db.SaveChanges();
            TempData["Success"] = "Đã đánh dấu tin là đã bán/cho thuê.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteImage(int id)
        {
            var img = db.Set<PropertyImage>().Find(id);
            if (img == null) return HttpNotFound();
            var p = FindOwned(img.PropertyId);
            if (p == null) return HttpNotFound();

            bool wasPrimary = img.IsPrimary;
            int propertyId = img.PropertyId;
            DeleteUploadedFile(img.ImageUrl);
            db.Set<PropertyImage>().Remove(img);
            db.SaveChanges();

            if (wasPrimary)
            {
                var next = db.Set<PropertyImage>().Where(i => i.PropertyId == propertyId).OrderBy(i => i.SortOrder).FirstOrDefault();
                if (next != null) { next.IsPrimary = true; db.SaveChanges(); }
            }
            return RedirectToAction("Edit", new { id = propertyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetPrimaryImage(int id)
        {
            var img = db.Set<PropertyImage>().Find(id);
            if (img == null) return HttpNotFound();
            var p = FindOwned(img.PropertyId);
            if (p == null) return HttpNotFound();

            foreach (var i in db.Set<PropertyImage>().Where(i => i.PropertyId == img.PropertyId).ToList())
                i.IsPrimary = (i.PropertyImageId == id);
            db.SaveChanges();
            return RedirectToAction("Edit", new { id = img.PropertyId });
        }

        // JSON: quan/huyen theo tinh (cho dropdown phu thuoc)
        [HttpGet]
        public JsonResult Districts(int provinceId)
        {
            var items = db.Set<District>().Where(d => d.ProvinceId == provinceId)
                .OrderBy(d => d.Name).Select(d => new { id = d.DistrictId, name = d.Name }).ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        // ===== helper =====
        private Property FindOwned(int id)
        {
            int? uid = CurrentUserId();
            if (!uid.HasValue) return null;
            bool admin = User.IsInRole("admin");
            return db.Properties.Include(p => p.District).Include(p => p.Images).Include(p => p.PropertyAmenities)
                .FirstOrDefault(p => p.PropertyId == id &&
                    (p.OwnerUserId == uid.Value || (admin && p.CreatedByUserId == uid.Value)));
        }

        private void CheckDistrict(ListingFormViewModel vm)
        {
            if (vm.DistrictId.HasValue && vm.ProvinceId.HasValue &&
                !db.Set<District>().Any(d => d.DistrictId == vm.DistrictId.Value && d.ProvinceId == vm.ProvinceId.Value))
            {
                ModelState.AddModelError("DistrictId", "Quận/huyện không thuộc tỉnh/thành đã chọn.");
            }
        }

        private void CheckAssignedUser(ListingFormViewModel vm, int? currentOwnerId)
        {
            if (!User.IsInRole("admin")) return;
            if (!vm.AssignedUserId.HasValue)
            {
                ModelState.AddModelError("AssignedUserId", "Vui lòng chọn tài khoản được gán tin đăng.");
                return;
            }
            var target = db.Users.Find(vm.AssignedUserId.Value);
            bool ok = target != null && target.Role != "admin"
                      && (target.Status == "active" || target.UserId == currentOwnerId);
            if (!ok)
                ModelState.AddModelError("AssignedUserId", "Tài khoản được chọn không hợp lệ.");
        }
        private void ApplyForm(Property p, ListingFormViewModel vm)
        {
            p.Title = vm.Title.Trim();
            p.PropertyTypeId = vm.PropertyTypeId.Value;
            p.TransactionTypeId = vm.TransactionTypeId.Value;
            p.DistrictId = vm.DistrictId.Value;
            p.Price = vm.Price.Value;
            p.Area = vm.Area.Value;
            p.LegalStatusId = vm.LegalStatusId.Value;
            p.FurnitureStatusId = vm.FurnitureStatusId.Value;
            p.Bedrooms = vm.Bedrooms;
            p.Bathrooms = vm.Bathrooms;
            p.Floors = vm.Floors;
            p.Description = vm.Description;
        }

        private void SaveAmenities(int propertyId, int[] ids)
        {
            var set = db.Set<PropertyAmenity>();
            set.RemoveRange(set.Where(a => a.PropertyId == propertyId).ToList());
            db.SaveChanges();
            if (ids != null)
            {
                foreach (int aid in ids.Distinct())
                    set.Add(new PropertyAmenity { PropertyId = propertyId, AmenityId = aid });
                db.SaveChanges();
            }
        }

        // Tra ve so anh bi bo qua (sai dinh dang, qua 5MB/anh, hoac vuot tong dung luong cua tin)
        private int SaveImages(int propertyId, IEnumerable<HttpPostedFileBase> images)
        {
            int skipped = 0;
            if (images == null) return 0;

            string dir = Server.MapPath("~/Content/uploads");
            Directory.CreateDirectory(dir);

            var set = db.Set<PropertyImage>();
            var existing = set.Where(i => i.PropertyId == propertyId).ToList();

            // Tong dung luong anh dang co cua tin (tinh tu file tren dia)
            long total = 0;
            foreach (var e in existing)
            {
                try
                {
                    string path = Server.MapPath(e.ImageUrl);
                    if (System.IO.File.Exists(path)) total += new FileInfo(path).Length;
                }
                catch { }
            }

            int order = existing.Count == 0 ? -1 : existing.Max(i => i.SortOrder);
            bool hasPrimary = existing.Any(i => i.IsPrimary);

            foreach (var f in images)
            {
                if (f == null || f.ContentLength == 0) continue;
                string ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                if (!AllowedExt.Contains(ext) || f.ContentLength > MaxImageBytes || total + f.ContentLength > MaxTotalBytes)
                {
                    skipped++;
                    continue;
                }

                string name = Guid.NewGuid().ToString("N") + ext;
                f.SaveAs(Path.Combine(dir, name));
                total += f.ContentLength;
                set.Add(new PropertyImage
                {
                    PropertyId = propertyId,
                    ImageUrl = "~/Content/uploads/" + name,
                    IsPrimary = !hasPrimary,
                    SortOrder = ++order
                });
                hasPrimary = true;
            }
            db.SaveChanges();
            return skipped;
        }

        private void PopulateLists(ListingFormViewModel vm)
        {
            vm.PropertyTypeList = db.Set<PropertyType>().OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.PropertyTypeId.ToString(), Text = x.Name }).ToList();
            vm.TransactionTypeList = db.Set<TransactionType>().OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.TransactionTypeId.ToString(), Text = x.Name }).ToList();
            vm.ProvinceList = db.Set<Province>().OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.ProvinceId.ToString(), Text = x.Name }).ToList();
            vm.LegalStatusList = db.Set<LegalStatus>().OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.LegalStatusId.ToString(), Text = x.Name }).ToList();
            vm.FurnitureStatusList = db.Set<FurnitureStatus>().OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.FurnitureStatusId.ToString(), Text = x.Name }).ToList();

            int pid = vm.ProvinceId ?? 0;
            vm.DistrictList = db.Set<District>().Where(d => d.ProvinceId == pid).OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.DistrictId.ToString(), Text = d.Name }).ToList();

            vm.AmenityList = db.Set<Amenity>().OrderBy(a => a.Name).ToList();
            
            if (User.IsInRole("admin"))
            {
                vm.IsAdmin = true;
                int cur = vm.AssignedUserId ?? 0;
                vm.UserList = db.Users
                    .Where(u => u.Role != "admin" && (u.Status == "active" || u.UserId == cur))
                    .OrderBy(u => u.FullName).ToList()
                    .Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = u.FullName + " (" + u.Email + ")" })
                    .ToList();
            }
        }
    }
}