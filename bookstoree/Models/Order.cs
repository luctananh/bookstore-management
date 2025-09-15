using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace bookstoree.Models
{
    public class Order
    {
        [Key]
        [DisplayName("Mã đơn hàng")]
        public int OrderId { get; set; }
        [DisplayName("Mã khách hàng")]
        public int UserId { get; set; }
        [DisplayName("Ngày đặt")]
        public DateTime OrderDate { get; set; }
        [DisplayName("Trạng thái")]
        public string? Status { get; set; } // e.g., Pending, Shipped, Delivered, Cancelled
        [DisplayName("Phương thức thanh toán")]
        public string? PaymentMethod { get; set; } // e.g., Credit Card, PayPal, etc.
        [ForeignKey("Discount")]
        [DisplayName("Mã giảm giá")]
        public string? DiscountCode { get; set; } // Optional discount code
        [DisplayName("Tổng tiền (VNĐ)")]
        public int TotalAmount { get; set; } // Total amount after applying discounts and taxes
        public virtual User? User { get; set; }
        public virtual DiscountCode? Discount { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    }
}
