using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bookstoree.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public string? Status { get; set; } // e.g., Pending, Shipped, Delivered, Cancelled
        public string? PaymentMethod { get; set; } // e.g., Credit Card, PayPal, etc.
        [ForeignKey("Discount")]
        public string? DiscountCode { get; set; } // Optional discount code
        public decimal TotalAmount { get; set; } // Total amount after applying discounts and taxes
        public virtual User? User { get; set; }
        public virtual DiscountCode? Discount { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    }
}
