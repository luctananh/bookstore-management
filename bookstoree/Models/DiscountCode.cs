using System.ComponentModel.DataAnnotations;

namespace bookstoree.Models
{
    public class DiscountCode
    {
        [Key]
        public string DiscountCodeId { get; set; } // Unique identifier for the discount code
        public string? Description { get; set; } // Description of the discount code
        public string? DiscountType { get; set; } = "Percent";// Type of discount (e.g., percentage, fixed amount)
        public decimal Value { get; set; } // Value of the discount (e.g., 10% or $5)
        public decimal MinimumOrder { get; set; } // Minimum order amount to apply the discount
        public DateTime StartDate { get; set; } // Start date of the discount code validity
        public DateTime EndDate { get; set; } // End date of the discount code validity
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    }
}
