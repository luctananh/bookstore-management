using System.ComponentModel.DataAnnotations;

namespace bookstoree.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? PasswordHash { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "Customer"; // "Admin", "Staff", "Customer"
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    }
}
 