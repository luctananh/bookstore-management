using bookstoree.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace bookstoree.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        public Order Order { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; } = new List<OrderDetailViewModel>();

        public OrderCreateViewModel()
        {
            // Initialize with at least one OrderDetailViewModel for the form
            OrderDetails.Add(new OrderDetailViewModel());
        }
    }

    public class OrderDetailViewModel
    {
        public int BookId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } // Price at the time of order
    }
}
