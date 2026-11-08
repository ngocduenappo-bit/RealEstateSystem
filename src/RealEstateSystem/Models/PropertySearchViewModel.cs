using System.Collections.Generic;
using System.Web.Mvc;
using RealEstateSystem.Models.ViewModels;

namespace RealEstateSystem.Models.ViewModels
{
    public class PagingInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages
        {
            get { return PageSize <= 0 ? 0 : (int)System.Math.Ceiling((double)TotalItems / PageSize); }
        }
    }
}

namespace RealEstateSystem.Models
{
    public class PropertySearchViewModel
    {
        public string Keyword { get; set; }
        public int? TransactionTypeId { get; set; }
        public int? PropertyTypeId { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? MinArea { get; set; }
        public decimal? MaxArea { get; set; }
        public int? Bedrooms { get; set; }

        public IEnumerable<SelectListItem> TransactionTypeList { get; set; }
        public IEnumerable<SelectListItem> PropertyTypeList { get; set; }
        public IEnumerable<SelectListItem> CityList { get; set; }

        public List<Property> Results { get; set; }
        public PagingInfo Paging { get; set; }
    }
}