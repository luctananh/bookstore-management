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
using bookstoree.Models.ViewModels;

namespace bookstoree.Controllers
{
    public class OrdersController : Controller
    {
        private readonly bookstoreeContext _context;

        public OrdersController(bookstoreeContext context)
        {
            _context = context;
        }

        // GET: Orders
        [Authorize] // Allow any authenticated user
        public async Task<IActionResult> Index(string searchString, string searchField)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentField"] = searchField;

            var searchFields = new Dictionary<string, string>
            {
                { "OrderId", "Mã đơn hàng" },
                { "UserName", "Tên khách hàng" },
                { "Status", "Trạng thái" }
            };
            ViewData["SearchFields"] = searchFields;

            IQueryable<Order> orders = _context.Order.Include(o => o.Discount).Include(o => o.User);

            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    orders = orders.Where(o => o.UserId == currentUserId.Value);
                }
                else
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                switch (searchField)
                {
                    case "OrderId":
                        orders = orders.Where(s => s.OrderId.ToString().Contains(searchString));
                        break;
                    case "UserName":
                        orders = orders.Where(s => s.User.FullName.Contains(searchString));
                        break;
                    case "Status":
                        orders = orders.Where(s => s.Status.Contains(searchString));
                        break;
                    default:
                        orders = orders.Where(s => s.OrderId.ToString().Contains(searchString)
                                               || s.User.FullName.Contains(searchString)
                                               || s.Status.Contains(searchString));
                        break;
                }
            }

            return View(await orders.ToListAsync());
        }

        // GET: Orders/Details/5
        [Authorize] // Allow any authenticated user
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.Discount)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            // Ownership check for Staff
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || order.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            // Admin users can view any order

            return View(order);
        }

        // GET: Orders/Create
        [Authorize(Roles = "Admin,Staff")] // Allow Admin and Staff to create
        public IActionResult Create()
        {
            var viewModel = new OrderCreateViewModel();
            ViewData["DiscountCode"] = new SelectList(_context.DiscountCode, "DiscountCodeId", "DiscountCodeId");
            ViewData["Books"] = new SelectList(_context.Book, "BookId", "Title"); // For selecting books in order details

            // UserId will be set automatically in POST action to the logged-in user's ID
            // No need to select it in the view
            return View(viewModel);
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")] // Allow Admin and Staff to create
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Set OrderDate if not provided by the form (e.g., if hidden)
                if (viewModel.Order.OrderDate == default(DateTime))
                {
                    viewModel.Order.OrderDate = DateTime.Now;
                }

                // Always set UserId to the logged-in user's ID
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    viewModel.Order.UserId = currentUserId.Value;
                }
                else
                {
                    // User not logged in or no valid UserId claim, deny creation
                    ModelState.AddModelError(string.Empty, "User ID not found. Please log in.");
                    ViewData["DiscountCode"] = new SelectList(_context.DiscountCode, "DiscountCodeId", "DiscountCodeId", viewModel.Order.DiscountCode);
                    ViewData["Books"] = new SelectList(_context.Book, "BookId", "Title");
                    return View(viewModel);
                }

                _context.Add(viewModel.Order);
                await _context.SaveChangesAsync(); // Save order to get OrderId

                // Add OrderDetails
                foreach (var item in viewModel.OrderDetails)
                {
                    if (item.BookId > 0 && item.Quantity > 0) // Ensure valid detail
                    {
                        var book = await _context.Book.FindAsync(item.BookId);
                        if (book != null)
                        {
                            var orderDetail = new OrderDetail
                            {
                                OrderId = viewModel.Order.OrderId,
                                BookId = item.BookId,
                                Quantity = item.Quantity,
                                UnitPrice = book.Price // Use current book price
                            };
                            _context.Add(orderDetail);
                        }
                    }
                }
                await _context.SaveChangesAsync(); // Save order details

                return RedirectToAction(nameof(Index));
            }

            // If ModelState is not valid, re-populate ViewData and return view
            ViewData["DiscountCode"] = new SelectList(_context.DiscountCode, "DiscountCodeId", "DiscountCodeId", viewModel.Order.DiscountCode);
            ViewData["Books"] = new SelectList(_context.Book, "BookId", "Title");
            return View(viewModel);
        }

        // GET: Orders/Edit/5
        [Authorize] // Allow any authenticated user
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Ownership check for Staff
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || order.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            // Admin users can edit any order

            ViewData["DiscountCode"] = new SelectList(_context.DiscountCode, "DiscountCodeId", "DiscountCodeId", order.DiscountCode);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "UserId", order.UserId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize] // Allow any authenticated user
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,UserId,OrderDate,Status,PaymentMethod,DiscountCode,TotalAmount")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            // Ownership check for Staff
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                // Fetch the original order to check ownership
                var originalOrder = await _context.Order.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == id);
                if (originalOrder == null || originalOrder.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
                // Ensure UserId is not changed by Staff
                order.UserId = currentUserId.Value;
            }
            // Admin users can edit any order

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
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
            ViewData["DiscountCode"] = new SelectList(_context.DiscountCode, "DiscountCodeId", "DiscountCodeId", order.DiscountCode);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "UserId", order.UserId);
            return View(order);
        }

        // GET: Orders/Delete/5
        [Authorize] // Allow any authenticated user
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.Discount)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            // Ownership check for Staff
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || order.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            // Admin users can delete any order

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize] // Allow any authenticated user
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound(); // Order not found
            }

            // Ownership check for Staff
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || order.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            // Admin users can delete any order

            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderId == id);
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
