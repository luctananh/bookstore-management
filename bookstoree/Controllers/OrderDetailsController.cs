using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using bookstoree.Data;
using bookstoree.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Added for FindFirstValue

namespace bookstoree.Controllers
{
    public class OrderDetailsController : Controller
    {
        private readonly bookstoreeContext _context;

        public OrderDetailsController(bookstoreeContext context)
        {
            _context = context;
        }

        // GET: OrderDetails
        [Authorize] // Allow any authenticated user
        public async Task<IActionResult> Index(string searchString, string searchField)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentField"] = searchField;

            var searchFields = new Dictionary<string, string>
            {
                { "OrderId", "Mã đơn hàng" },
                { "BookTitle", "Tên sách" }
            };
            ViewData["SearchFields"] = searchFields;

            IQueryable<OrderDetail> orderDetails = _context.OrderDetail.Include(o => o.Book).Include(o => o.Order);

            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    orderDetails = orderDetails.Where(od => od.Order.UserId == currentUserId.Value);
                }
                else
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                switch (searchField)
                {
                    case "OrderId":
                        orderDetails = orderDetails.Where(od => od.Order.OrderId.ToString().Contains(searchString));
                        break;
                    case "BookTitle":
                        orderDetails = orderDetails.Where(od => od.Book.Title.Contains(searchString));
                        break;
                    default:
                        orderDetails = orderDetails.Where(od =>
                            od.Order.OrderId.ToString().Contains(searchString) ||
                            od.Book.Title.Contains(searchString));
                        break;
                }
            }

            return View(await orderDetails.ToListAsync());
        }

        // GET: OrderDetails/Details/5
        [Authorize] // Allow any authenticated user
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderDetail = await _context.OrderDetail
                .Include(o => o.Book)
                .Include(o => o.Order) // Include Order to check its UserId
                .FirstOrDefaultAsync(m => m.OrderDetailId == id);
            if (orderDetail == null)
            {
                return NotFound();
            }

            // Ownership check for Staff
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || orderDetail.Order.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            // Admin users can view any order detail

            return View(orderDetail);
        }

        // GET: OrderDetails/Create
        [Authorize(Roles = "Admin,Staff")] // Allow Admin and Staff to create
        public IActionResult Create()
        {
            ViewData["BookId"] = new SelectList(_context.Book, "BookId", "Title"); // Changed to Title for better display

            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    // Staff can only create order details for their own orders
                    ViewData["OrderId"] = new SelectList(_context.Order.Where(o => o.UserId == currentUserId.Value), "OrderId", "OrderId");
                }
                else
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            else // Admin or other roles
            {
                ViewData["OrderId"] = new SelectList(_context.Order, "OrderId", "OrderId");
            }
            
            return View();
        }

        // POST: OrderDetails/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")] // Allow Admin and Staff to create
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderDetailId,OrderId,BookId,Quantity,UnitPrice")] OrderDetail orderDetail)
        {
            // Ownership check for Staff on the parent order
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
                var parentOrder = await _context.Order.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderDetail.OrderId);
                if (parentOrder == null || parentOrder.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            // Admin users can create for any order

            if (ModelState.IsValid)
            {
                _context.Add(orderDetail);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Chi tiết đơn hàng đã được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookId"] = new SelectList(_context.Book, "BookId", "Title", orderDetail.BookId); // Changed to Title
            ViewData["OrderId"] = new SelectList(_context.Order, "OrderId", "OrderId", orderDetail.OrderId);
            return View(orderDetail);
        }

        // GET: OrderDetails/Edit/5
        [Authorize] // Allow any authenticated user
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderDetail = await _context.OrderDetail.Include(od => od.Order).FirstOrDefaultAsync(od => od.OrderDetailId == id);
            if (orderDetail == null)
            {
                return NotFound();
            }

            // Ownership check for Staff
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || orderDetail.Order.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            // Admin users can edit any order detail

            ViewData["BookId"] = new SelectList(_context.Book, "BookId", "Title", orderDetail.BookId); // Changed to Title
            ViewData["OrderId"] = new SelectList(_context.Order, "OrderId", "OrderId", orderDetail.OrderId);
            return View(orderDetail);
        }

        // POST: OrderDetails/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize] // Allow any authenticated user
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderDetailId,OrderId,BookId,Quantity,UnitPrice")] OrderDetail orderDetail)
        {
            if (id != orderDetail.OrderDetailId)
            {
                return NotFound();
            }

            // Ownership check for Staff on the parent order
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
                var parentOrder = await _context.Order.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderDetail.OrderId);
                if (parentOrder == null || parentOrder.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            // Admin users can edit any order detail

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderDetail);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Chi tiết đơn hàng đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderDetailExists(orderDetail.OrderDetailId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookId"] = new SelectList(_context.Book, "BookId", "Title", orderDetail.BookId); // Changed to Title
            ViewData["OrderId"] = new SelectList(_context.Order, "OrderId", "OrderId", orderDetail.OrderId);
            return View(orderDetail);
        }

        // GET: OrderDetails/Delete/5
        [Authorize] // Allow any authenticated user
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderDetail = await _context.OrderDetail
                .Include(o => o.Book)
                .Include(o => o.Order) // Include Order to check its UserId
                .FirstOrDefaultAsync(m => m.OrderDetailId == id);
            if (orderDetail == null)
            {
                return NotFound();
            }

            // Ownership check for Staff
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || orderDetail.Order.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            // Admin users can delete any order detail

            return View(orderDetail);
        }

        // POST: OrderDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize] // Allow any authenticated user
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderDetail = await _context.OrderDetail.Include(od => od.Order).FirstOrDefaultAsync(od => od.OrderDetailId == id);
            if (orderDetail == null)
            {
                return NotFound(); // Order detail not found
            }

            // Ownership check for Staff
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || orderDetail.Order.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            // Admin users can delete any order detail

            _context.OrderDetail.Remove(orderDetail);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Chi tiết đơn hàng đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool OrderDetailExists(int id)
        {
            return _context.OrderDetail.Any(e => e.OrderDetailId == id);
        }

        private int? GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}
