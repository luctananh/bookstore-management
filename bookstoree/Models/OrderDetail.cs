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
        public int Quantity { get; set; } // Quantity of the book in the order
        [DisplayName("Đơn giá")]
        public int UnitPrice { get; set; } // Price of the book at the time of order
        public virtual Order? Order { get; set; }
        public virtual Book? Book { get; set; }

    }
}