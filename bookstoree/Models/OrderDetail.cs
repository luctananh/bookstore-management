using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace bookstoree.Models
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; } // Foreign key to Orders
        public int BookId { get; set; } // Foreign key to Books
        public int Quantity { get; set; } // Quantity of the book in the order
                public int UnitPrice { get; set; } // Price of the book at the time of order
        public virtual Order? Order { get; set; }
        public virtual Book? Book { get; set; }

    }
}