using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using bookstoree.Models;

namespace bookstoree.Data
{
    public class bookstoreeContext : DbContext
    {
        public bookstoreeContext (DbContextOptions<bookstoreeContext> options)
            : base(options)
        {
        }

        public DbSet<bookstoree.Models.Category> Category { get; set; } = default!;
        public DbSet<bookstoree.Models.Book> Book { get; set; } = default!;
        public DbSet<bookstoree.Models.User> User { get; set; } = default!;
        public DbSet<bookstoree.Models.Order> Order { get; set; } = default!;
        public DbSet<bookstoree.Models.OrderDetail> OrderDetail { get; set; } = default!;
        public DbSet<bookstoree.Models.DiscountCode> DiscountCode { get; set; } = default!;
    }
}
