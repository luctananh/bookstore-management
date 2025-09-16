using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace bookstoree.Models
{
    public class User
    {
        [Key]
        [DisplayName("Mã người dùng")]
        public int UserId { get; set; }
        [DisplayName("Tên đăng nhập")]
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        public string? UserName { get; set; }
        [DisplayName("Mật khẩu")]
        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        public string? PasswordHash { get; set; }
        [DisplayName("Họ và tên")]
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        public string? FullName { get; set; }
        [DisplayName("Địa chỉ Email")]
        [Required(ErrorMessage = "Địa chỉ Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
        public string? Email { get; set; }
        [DisplayName("Số điện thoại")]
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }
        [DisplayName("Vai trò")]
        public string Role { get; set; } = "Admin"; // "Admin", "Staff"
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    }
}
 