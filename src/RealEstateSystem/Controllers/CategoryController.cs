using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using RealEstateSystem.Models.ViewModels;

namespace RealEstateSystem.Controllers
{
    [RoleAuthorize(Roles = "admin")]
    public class CategoryController : BaseController
    {
        // type -> { bang, cot khoa, bang dang dung, cot dung, tieu de }
        // (danh sach co dinh trong code, khong nhan ten bang tu nguoi dung)
        private static readonly Dictionary<string, string[]> Map = new Dictionary<string, string[]>
        {
            { "propertytype",    new[] { "property_types", "property_type_id", "properties", "property_type_id", "Loại bất động sản" } },
            { "transactiontype", new[] { "transaction_types", "transaction_type_id", "properties", "transaction_type_id", "Hình thức giao dịch" } },
            { "province",        new[] { "provinces", "province_id", "districts", "province_id", "Tỉnh/Thành" } },
            { "district",        new[] { "districts", "district_id", "properties", "district_id", "Quận/Huyện" } },
            { "amenity",         new[] { "amenities", "amenity_id", "property_amenities", "amenity_id", "Tiện ích" } },
            { "legalstatus",     new[] { "legal_statuses", "legal_status_id", "properties", "legal_status_id", "Giấy tờ pháp lý" } },
            { "furniturestatus", new[] { "furniture_statuses", "furniture_status_id", "properties", "furniture_status_id", "Tình trạng nội thất" } }
        };

        public ActionResult Index(string type = "propertytype", int? provinceId = null)
        {
            if (!Map.ContainsKey(type ?? "")) type = "propertytype";
            var m = Map[type];
            List<CategoryItem> items;

            if (type == "district")
            {
                string sql = "SELECT d.district_id AS Id, d.name AS Name, d.province_id AS ParentId, p.name AS ParentName, "
                    + "(SELECT COUNT(*) FROM properties x WHERE x.district_id = d.district_id) AS UsageCount "
                    + "FROM districts d JOIN provinces p ON p.province_id = d.province_id";
                items = provinceId.HasValue
                    ? db.Database.SqlQuery<CategoryItem>(sql + " WHERE d.province_id = @p0 ORDER BY p.name, d.name", provinceId.Value).ToList()
                    : db.Database.SqlQuery<CategoryItem>(sql + " ORDER BY p.name, d.name").ToList();
            }
            else
            {
                string sql = string.Format(
                    "SELECT t.{1} AS Id, t.name AS Name, (SELECT COUNT(*) FROM {2} u WHERE u.{3} = t.{1}) AS UsageCount FROM {0} t ORDER BY t.name",
                    m[0], m[1], m[2], m[3]);
                items = db.Database.SqlQuery<CategoryItem>(sql).ToList();
            }

            return View(new CategoryListViewModel
            {
                Type = type,
                Title = m[4],
                ProvinceId = provinceId,
                Items = items,
                ProvinceList = db.Database.SqlQuery<CategoryItem>("SELECT province_id AS Id, name AS Name FROM provinces ORDER BY name")
                    .ToList().Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList(),
                Tabs = Map.ToDictionary(kv => kv.Key, kv => kv.Value[4])
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(string type, string name, int? provinceId)
        {
            if (!Map.ContainsKey(type ?? "")) return HttpNotFound();
            var m = Map[type];
            name = (name ?? "").Trim();

            if (name.Length == 0) return Fail(type, provinceId, "Vui lòng nhập tên.");

            if (type == "district")
            {
                if (!provinceId.HasValue) return Fail(type, provinceId, "Vui lòng chọn tỉnh/thành.");
                if (Count("SELECT COUNT(*) FROM districts WHERE name = @p0 AND province_id = @p1", name, provinceId.Value) > 0)
                    return Fail(type, provinceId, "Quận/huyện này đã tồn tại trong tỉnh/thành đã chọn.");
                db.Database.ExecuteSqlCommand("INSERT INTO districts (province_id, name) VALUES (@p0, @p1)", provinceId.Value, name);
            }
            else
            {
                if (Count("SELECT COUNT(*) FROM " + m[0] + " WHERE name = @p0", name) > 0)
                    return Fail(type, provinceId, "Tên này đã tồn tại.");
                db.Database.ExecuteSqlCommand("INSERT INTO " + m[0] + " (name) VALUES (@p0)", name);
            }

            TempData["Success"] = "Đã thêm mới.";
            return RedirectToAction("Index", new { type = type, provinceId = provinceId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Update(string type, int id, string name, int? provinceId)
        {
            if (!Map.ContainsKey(type ?? "")) return HttpNotFound();
            var m = Map[type];
            name = (name ?? "").Trim();

            if (name.Length == 0) return Fail(type, provinceId, "Vui lòng nhập tên.");

            string dupSql = "SELECT COUNT(*) FROM " + m[0] + " WHERE name = @p0 AND " + m[1] + " <> @p1";
            if (type == "district")
                dupSql += " AND province_id = (SELECT province_id FROM districts WHERE district_id = @p1)";
            if (Count(dupSql, name, id) > 0) return Fail(type, provinceId, "Tên này đã tồn tại.");

            db.Database.ExecuteSqlCommand("UPDATE " + m[0] + " SET name = @p0 WHERE " + m[1] + " = @p1", name, id);

            TempData["Success"] = "Đã cập nhật.";
            return RedirectToAction("Index", new { type = type, provinceId = provinceId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(string type, int id, int? provinceId)
        {
            if (!Map.ContainsKey(type ?? "")) return HttpNotFound();
            var m = Map[type];

            int used = Count("SELECT COUNT(*) FROM " + m[2] + " WHERE " + m[3] + " = @p0", id);
            if (used > 0)
                return Fail(type, provinceId, "Không thể xóa vì đang được sử dụng (" + used + " bản ghi liên quan).");

            db.Database.ExecuteSqlCommand("DELETE FROM " + m[0] + " WHERE " + m[1] + " = @p0", id);

            TempData["Success"] = "Đã xóa.";
            return RedirectToAction("Index", new { type = type, provinceId = provinceId });
        }

        private int Count(string sql, params object[] args)
        {
            return db.Database.SqlQuery<int>(sql, args).First();
        }

        private ActionResult Fail(string type, int? provinceId, string message)
        {
            TempData["Error"] = message;
            return RedirectToAction("Index", new { type = type, provinceId = provinceId });
        }
    }
}