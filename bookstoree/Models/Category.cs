using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace bookstoree.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        [DisplayName("Tên danh mục")]
        [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
        public string? CategoryName { get; set; }
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    }
}
