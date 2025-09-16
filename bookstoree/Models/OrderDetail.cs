using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace bookstoree.Models
{
    public class OrderDetail
    {
        [Key]
        [DisplayName("Mã chi tiết đơn hàng")]
        public int OrderDetailId { get; set; }
        [DisplayName("Mã đơn hàng")]
        public int OrderId { get; set; } // Foreign key to Orders
        [DisplayName("Sách")]
        public int BookId { get; set; } // Foreign key to Books
        [DisplayName("Số lượng")]
        public int Quantity { get; set; }

        [DisplayName("Đơn giá")]
        public decimal UnitPrice { get; set; }

        [DisplayName("Mã cửa hàng")]
        public int? StoreId { get; set; }
        [ForeignKey("StoreId")]
        public virtual Store? Store { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
        [ForeignKey("BookId")]
        public virtual Book? Book { get; set; }

    }
}