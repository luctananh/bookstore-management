using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace bookstoree.Models.ViewModels
{
    public class StoreRegistrationViewModel
    {
        // Store Details
        [Required(ErrorMessage = "Tên cửa hàng là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên cửa hàng không được vượt quá 100 ký tự.")]
        [DisplayName("Tên cửa hàng")]
        public string StoreName { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự.")]
        [DisplayName("Địa chỉ cửa hàng")]
        public string? StoreAddress { get; set; }

        [Phone(ErrorMessage = "Số điện thoại cửa hàng không hợp lệ.")]
        [DisplayName("Số điện thoại cửa hàng")]
        public string? StorePhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Email liên hệ cửa hàng không hợp lệ.")]
        [DisplayName("Email liên hệ cửa hàng")]
        public string? StoreContactEmail { get; set; }

        // Admin User Details
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        [DisplayName("Tên đăng nhập Admin")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        [DisplayName("Mật khẩu Admin")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
        [DisplayName("Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [DisplayName("Họ và tên Admin")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Địa chỉ Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
        [DisplayName("Email Admin")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [DisplayName("Số điện thoại Admin")]
        public string PhoneNumber { get; set; }
    }
}
