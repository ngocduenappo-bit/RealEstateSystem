using System.Collections.Generic;

namespace RealEstateSystem.Models
{
    public class InquiryFormViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Message { get; set; }
    }

    public class PropertyDetailViewModel
    {
        public Property Property { get; set; }
        public InquiryFormViewModel Inquiry { get; set; }
        public List<Property> RelatedProperties { get; set; }
    }
}