using System.Collections.Generic;
using System.Web.Mvc;

namespace RealEstateSystem.Models
{
    public class SummaryItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class CitySummary
    {
        public string City { get; set; }
        public int Count { get; set; }
    }

    public class HomeViewModel
    {
        public List<Property> FeaturedProperties { get; set; }
        public List<Property> LatestProperties { get; set; }
        public List<SummaryItem> TransactionSummaries { get; set; }
        public List<SummaryItem> PropertyTypeSummaries { get; set; }
        public List<CitySummary> CitySummaries { get; set; }
        public IEnumerable<SelectListItem> PropertyTypeList { get; set; }
    }
}