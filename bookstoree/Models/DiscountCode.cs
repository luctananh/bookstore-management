using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace bookstoree.Models
{
    public class DiscountCode
    {
        [Key]
        [DisplayName("Mã giảm giá")]
        [Required(ErrorMessage = "Mã giảm giá là bắt buộc.")]
        public string DiscountCodeId { get; set; } // Unique identifier for the discount code
        [DisplayName("Mô tả")]
        public string? Description { get; set; } // Description of the discount code
        [DisplayName("Loại giảm giá")]
        public string? DiscountType { get; set; } = "Percent";// Type of discount (e.g., percentage, fixed amount)
        
        [DisplayName("Giá trị")]
        // Value of the discount. If DiscountType is "Percent", this is a percentage. If "Fixed", this is a currency amount.
        [Required(ErrorMessage = "Trường Giá trị là bắt buộc.")]
        public int Value { get; set; } 
        
        [DisplayName("Đơn hàng tối thiểu (VNĐ)")]
        [Required(ErrorMessage = "Trường Đơn hàng tối thiểu (VNĐ) là bắt buộc.")]
        public int MinimumOrder { get; set; } // Minimum order amount to apply the discount
        [DisplayName("Ngày bắt đầu")]
        [Required(ErrorMessage = "Trường Ngày bắt đầu là bắt buộc.")]
        public DateTime StartDate { get; set; } // Start date of the discount code validity
        [DisplayName("Ngày kết thúc")]
        [Required(ErrorMessage = "Trường Ngày kết thúc là bắt buộc.")]
        public DateTime EndDate { get; set; } // End date of the discount code validity
        public decimal DiscountAmount { get; set; }

        [DisplayName("Mã cửa hàng")]
        public int? StoreId { get; set; }
        [ForeignKey("StoreId")]
        public virtual Store? Store { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    }
}
