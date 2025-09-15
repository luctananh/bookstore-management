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
        public string? UserName { get; set; }
        [DisplayName("Mật khẩu")]
        public string? PasswordHash { get; set; }
        [DisplayName("Họ và tên")]
        public string? FullName { get; set; }
        [DisplayName("Địa chỉ Email")]
        public string? Email { get; set; }
        [DisplayName("Số điện thoại")]
        public string? PhoneNumber { get; set; }
        [DisplayName("Vai trò")]
        public string Role { get; set; } = "Customer"; // "Admin", "Staff", "Customer"
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    }
}
 