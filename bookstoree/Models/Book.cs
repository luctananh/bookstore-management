using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace bookstoree.Models
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }
        [DisplayName("Mã ISBN")]
        public string? ISBN { get; set; }
        [DisplayName("Tên sách")]
        public string? Title { get; set; }
        [DisplayName("Tác giả")]
        public string? Author { get; set; }
        [DisplayName("Nhà xuất bản")]
        public string? Publisher { get; set; }
        [DisplayName("Danh mục")]
        public int CategoryId { get; set; }
        [DisplayName("Giá (VNĐ)")]
        public int Price { get; set; }
        [DisplayName("Số lượng tồn kho")]
        public int StockQuantity { get; set; }
        [DisplayName("Đường dẫn hình ảnh")]
        public string? ImageUrl { get; set; }
        [DisplayName("Mô tả")]
        public string? Description { get; set; }
        [DisplayName("Ngày thêm")]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Navigation
        public virtual Category? Category { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
