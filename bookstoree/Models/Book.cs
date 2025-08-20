using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bookstoree.Models
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }
        public string? ISBN { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Publisher { get; set; }
        public int CategoryId { get; set; }
        public int Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }

        // Navigation
        public virtual Category? Category { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
