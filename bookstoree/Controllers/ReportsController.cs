using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using bookstoree.Data;

using bookstoree.Services;

namespace bookstoree.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly bookstoreeContext _context;
        private readonly CurrentStoreService _currentStoreService;

        public ReportsController(bookstoreeContext context, CurrentStoreService currentStoreService)
        {
            _context = context;
            _currentStoreService = currentStoreService;
        }

        public async Task<IActionResult> Index()
        {
            // Basic sales statistics
            var totalOrders = await _context.Order.CountAsync();
            var totalRevenue = await _context.Order.SumAsync(o => o.TotalAmount);
            var totalBooksSold = await _context.OrderDetail.SumAsync(od => od.Quantity);

            ViewData["TotalOrders"] = totalOrders;
            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["TotalBooksSold"] = totalBooksSold;

            return View();
        }

        // You can add more specific reports here, e.g., sales by category, top selling books
        public async Task<IActionResult> SalesByCategory()
        {
            var salesByCategory = await _context.OrderDetail
                .Include(od => od.Book)
                .ThenInclude(b => b.Category)
                .GroupBy(od => od.Book.Category.Name)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    TotalSales = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSales)
                .ToListAsync();

            return View(salesByCategory);
        }

        public async Task<IActionResult> TopSellingBooks()
        {
            var topSellingBooks = await _context.OrderDetail
                .Include(od => od.Book)
                .GroupBy(od => od.Book.Title)
                .Select(g => new
                {
                    BookTitle = g.Key,
                    TotalQuantitySold = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(10) // Top 10 books
                .ToListAsync();

            return View(topSellingBooks);
        }

        public async Task<IActionResult> SalesByUser()
        {
            var salesByUser = await _context.Order
                .Include(o => o.User)
                .GroupBy(o => o.User.FullName)
                .Select(g => new
                {
                    UserName = g.Key,
                    TotalSales = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSales)
                .ToListAsync();

            return View(salesByUser);
        }
    }
}
