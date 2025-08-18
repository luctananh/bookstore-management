using System.ComponentModel.DataAnnotations;

namespace bookstoree.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    }
}
