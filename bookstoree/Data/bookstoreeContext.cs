using Microsoft.EntityFrameworkCore;
using bookstoree.Models;
using bookstoree.Services;

namespace bookstoree.Data
{
    public class bookstoreeContext : DbContext
    {
        private readonly CurrentStoreService _currentStoreService;

        public bookstoreeContext (DbContextOptions<bookstoreeContext> options, CurrentStoreService currentStoreService)
            : base(options)
        {
            _currentStoreService = currentStoreService;
        }

        public DbSet<bookstoree.Models.Category> Category { get; set; } = default!;
        public DbSet<bookstoree.Models.Book> Book { get; set; } = default!;
        public DbSet<bookstoree.Models.User> User { get; set; } = default!;
        public DbSet<bookstoree.Models.Order> Order { get; set; } = default!;
        public DbSet<bookstoree.Models.OrderDetail> OrderDetail { get; set; } = default!;
        public DbSet<bookstoree.Models.DiscountCode> DiscountCode { get; set; } = default!;
        public DbSet<bookstoree.Models.Store> Store { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply global query filter for multi-tenancy
            modelBuilder.Entity<Book>().HasQueryFilter(b => b.StoreId == _currentStoreService.GetCurrentStoreId());
            modelBuilder.Entity<Category>().HasQueryFilter(c => c.StoreId == _currentStoreService.GetCurrentStoreId());
            modelBuilder.Entity<DiscountCode>().HasQueryFilter(dc => dc.StoreId == _currentStoreService.GetCurrentStoreId());
            modelBuilder.Entity<Order>().HasQueryFilter(o => o.StoreId == _currentStoreService.GetCurrentStoreId());
            modelBuilder.Entity<OrderDetail>().HasQueryFilter(od => od.StoreId == _currentStoreService.GetCurrentStoreId());
            modelBuilder.Entity<User>().HasQueryFilter(u => u.StoreId == _currentStoreService.GetCurrentStoreId());

            // Configure one-to-many relationship between Store and User
            modelBuilder.Entity<Store>()
                .HasMany(s => s.Users)
                .WithOne(u => u.Store)
                .HasForeignKey(u => u.StoreId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletion of store if users exist

            // Configure one-to-many relationship between Store and Book
            modelBuilder.Entity<Store>()
                .HasMany(s => s.Books)
                .WithOne(b => b.Store)
                .HasForeignKey(b => b.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Store and Category
            modelBuilder.Entity<Store>()
                .HasMany(s => s.Categories)
                .WithOne(c => c.Store)
                .HasForeignKey(c => c.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Store and Order
            modelBuilder.Entity<Store>()
                .HasMany(s => s.Orders)
                .WithOne(o => o.Store)
                .HasForeignKey(o => o.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Store and DiscountCode
            modelBuilder.Entity<Store>()
                .HasMany(s => s.DiscountCodes)
                .WithOne(dc => dc.Store)
                .HasForeignKey(dc => dc.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Category and Book
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Books)
                .WithOne(b => b.Category) // Specify the navigation property in Book
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Or Cascade, depending on desired behavior

            // Configure one-to-many relationship between Order and OrderDetail
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderDetails)
                .WithOne(od => od.Order)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many relationship between Book and OrderDetail
            modelBuilder.Entity<Book>()
                .HasMany(b => b.OrderDetails)
                .WithOne(od => od.Book)
                .HasForeignKey(od => od.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure unique index for UserName per StoreId
            modelBuilder.Entity<User>()
                .HasIndex(u => new { u.StoreId, u.UserName })
                .IsUnique();

            // Configure unique index for Email per StoreId
            modelBuilder.Entity<User>()
                .HasIndex(u => new { u.StoreId, u.Email })
                .IsUnique();
        }
    }
}
