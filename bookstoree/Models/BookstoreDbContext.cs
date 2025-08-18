using bookstoree.Models;
using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

public class BookstoreDbContext : DbContext
{
    public BookstoreDbContext(DbContextOptions<BookstoreDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Keys
        modelBuilder.Entity<DiscountCode>()
            .HasKey(dc => dc.DiscountCodeId);

        // Relationships
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Discount)
            .WithMany(d => d.Orders)
            .HasForeignKey(o => o.DiscountCode)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Book>()
            .HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Book)
            .WithMany(b => b.OrderDetails)
            .HasForeignKey(od => od.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes and uniqueness
        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique()
            .HasFilter("[UserName] IS NOT NULL");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        modelBuilder.Entity<Book>()
            .HasIndex(b => b.ISBN)
            .IsUnique()
            .HasFilter("[ISBN] IS NOT NULL");

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.CategoryName)
            .IsUnique()
            .HasFilter("[CategoryName] IS NOT NULL");

        modelBuilder.Entity<OrderDetail>()
            .HasIndex(od => new { od.OrderId, od.BookId })
            .IsUnique();

        // Decimal precision
        modelBuilder.Entity<Book>()
            .Property(b => b.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DiscountCode>()
            .Property(dc => dc.Value)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DiscountCode>()
            .Property(dc => dc.MinimumOrder)
            .HasPrecision(18, 2);

        // Check constraints
        modelBuilder.Entity<Book>()
            .HasCheckConstraint("CK_Book_Price_NonNegative", "[Price] >= 0");

        modelBuilder.Entity<Book>()
            .HasCheckConstraint("CK_Book_Stock_NonNegative", "[StockQuantity] >= 0");

        modelBuilder.Entity<OrderDetail>()
            .HasCheckConstraint("CK_OrderDetail_Quantity_Positive", "[Quantity] > 0");

        modelBuilder.Entity<OrderDetail>()
            .HasCheckConstraint("CK_OrderDetail_UnitPrice_NonNegative", "[UnitPrice] >= 0");

        modelBuilder.Entity<Order>()
            .HasCheckConstraint("CK_Order_TotalAmount_NonNegative", "[TotalAmount] >= 0");

        modelBuilder.Entity<DiscountCode>()
            .HasCheckConstraint("CK_DiscountCode_Value_NonNegative", "[Value] >= 0");

        modelBuilder.Entity<DiscountCode>()
            .HasCheckConstraint("CK_DiscountCode_MinimumOrder_NonNegative", "[MinimumOrder] >= 0");

        modelBuilder.Entity<DiscountCode>()
            .HasCheckConstraint("CK_DiscountCode_Dates", "[EndDate] >= [StartDate]");

        base.OnModelCreating(modelBuilder);
    }
}
