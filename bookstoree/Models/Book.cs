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
        [Required(ErrorMessage = "Vui lòng nhập mã ISBN")]
        public string? ISBN { get; set; }

        [DisplayName("Tên sách")]
        [Required(ErrorMessage = "Vui lòng nhập tên sách")]
        public string? Title { get; set; }

        [DisplayName("Tác giả")]
        [Required(ErrorMessage = "Vui lòng nhập tác giả")]
        public string? Author { get; set; }

        [DisplayName("Nhà xuất bản")]
        [Required(ErrorMessage = "Vui lòng nhập nhà xuất bản")]
        public string? Publisher { get; set; }

        [DisplayName("Danh mục")]
        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        [DisplayName("Giá (VNĐ)")]
        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Range(1, int.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public int Price { get; set; }

        [DisplayName("Số lượng tồn kho")]
        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không hợp lệ")]
        public int StockQuantity { get; set; }

        [DisplayName("Đường dẫn hình ảnh")]
        public string? ImageUrl { get; set; }

        [DisplayName("Mô tả")]
        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        public string? Description { get; set; }

        [DisplayName("Ngày thêm")]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Navigation
        public virtual Category? Category { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
