using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace bookstoree.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        [DisplayName("Tên danh mục")]

        public string? CategoryName { get; set; }
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    }
}
