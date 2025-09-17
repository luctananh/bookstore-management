using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore; // Added for [Index] attribute

namespace bookstoree.Models
{
    [Index(nameof(Name), IsUnique = true)] // Added for performance
    public class Store
    {
        [Key]
        [DisplayName("Mã cửa hàng")]
        public int StoreId { get; set; }

        [Required(ErrorMessage = "Tên cửa hàng là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên cửa hàng không được vượt quá 100 ký tự.")]
        [DisplayName("Tên cửa hàng")]
        public string Name { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự.")]
        [DisplayName("Địa chỉ")]
        public string? Address { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [DisplayName("Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
        [DisplayName("Email liên hệ")]
        public string? ContactEmail { get; set; }

        // Navigation property for books in this store
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
        // Navigation property for users in this store
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        // Navigation property for categories in this store
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        // Navigation property for orders in this store
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        // Navigation property for discount codes in this store
        public virtual ICollection<DiscountCode> DiscountCodes { get; set; } = new List<DiscountCode>();
    }
}
