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
        [Required(ErrorMessage = "Ngày đặt hàng là bắt buộc.")]
        public DateTime OrderDate { get; set; }
        [DisplayName("Trạng thái")]
        public string? Status { get; set; } // e.g., Pending, Shipped, Delivered, Cancelled
        [DisplayName("Phương thức thanh toán")]
        public string? PaymentMethod { get; set; } // e.g., Credit Card, PayPal, etc.
        public string? AppliedDiscountCode { get; set; } // Optional discount code string
        [DisplayName("Tổng tiền (VNĐ)")]
        public int TotalAmount { get; set; } // Total amount after applying discounts and taxes
        public string? DiscountCodeId { get; set; }

        [DisplayName("Mã cửa hàng")]
        public int? StoreId { get; set; }
        [ForeignKey("StoreId")]
        public virtual Store? Store { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        [ForeignKey("DiscountCodeId")]
        public virtual DiscountCode? DiscountCode { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    }
}
