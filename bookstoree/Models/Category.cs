using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace bookstoree.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        [DisplayName("Tên danh mục")]
        [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
        public string? Name { get; set; }

        [DisplayName("Mã cửa hàng")]
        public int? StoreId { get; set; }
        [ForeignKey("StoreId")]
        public virtual Store? Store { get; set; }

        public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    }
}
