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

            IQueryable<Order> orders = _context.Order.Include(o => o.Discount).Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book);

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
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
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
            var discounts = _context.DiscountCode.ToList();
            var discountList = discounts.Select(d => new { d.DiscountCodeId, d.Description }).ToList();
            discountList.Insert(0, new { DiscountCodeId = "", Description = "None" });
            ViewData["DiscountCode"] = new SelectList(discountList, "DiscountCodeId", "Description");

            ViewData["DiscountDetails"] = System.Text.Json.JsonSerializer.Serialize(
                discounts.ToDictionary(d => d.DiscountCodeId, d => new { d.DiscountType, d.Value, d.MinimumOrder }));

            var books = _context.Book.Select(b => new { b.BookId, b.Title, b.Price }).ToList();
            ViewData["BookListForJs"] = books;
            ViewData["Books"] = new SelectList(books, "BookId", "Title");
            ViewData["BookPrices"] = System.Text.Json.JsonSerializer.Serialize(books.ToDictionary(b => b.BookId, b => b.Price));

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

            var order = await _context.Order
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
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
            ViewData["DiscountCode"] = new SelectList(_context.DiscountCode, "DiscountCodeId", "DiscountCodeId", order.DiscountCode);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "UserId", order.UserId);

            var books = _context.Book.Select(b => new { b.BookId, b.Title, b.Price }).ToList();
            ViewData["BookListForJs"] = books;
            ViewData["Books"] = new SelectList(books, "BookId", "Title");
            ViewData["BookPrices"] = books.ToDictionary(b => b.BookId, b => b.Price);

            var discounts = _context.DiscountCode.ToList();
            ViewData["DiscountDetails"] = discounts.ToDictionary(d => d.DiscountCodeId, d => new { d.DiscountType, d.Value, d.MinimumOrder });

            var existingOrderDetailsForJson = order.OrderDetails.Select(od => new
            {
                od.OrderDetailId,
                od.BookId,
                od.Quantity,
                od.UnitPrice,
                Book = new { od.Book.Title } // Select only the title from Book
            }).ToList();
            ViewData["ExistingOrderDetailsJson"] = System.Text.Json.JsonSerializer.Serialize(existingOrderDetailsForJson);

            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize] // Allow any authenticated user
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order order, List<OrderDetail> OrderDetails)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            // Fetch the existing order with its details from the database
            var orderToUpdate = await _context.Order
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (orderToUpdate == null)
            {
                return NotFound();
            }

            // Ownership check for Staff
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || orderToUpdate.UserId != currentUserId.Value)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
                // Ensure UserId is not changed by Staff
                order.UserId = currentUserId.Value; // Keep the original user ID
            }
            // Admin users can edit any order

            // Update main order properties
            orderToUpdate.OrderDate = order.OrderDate;
            orderToUpdate.Status = order.Status;
            orderToUpdate.PaymentMethod = order.PaymentMethod;
            orderToUpdate.DiscountCode = order.DiscountCode;
            // TotalAmount will be recalculated

            // Handle OrderDetails
            // Remove existing details that are not in the new list
            _context.OrderDetail.RemoveRange(orderToUpdate.OrderDetails.Where(od => !OrderDetails.Any(newOd => newOd.OrderDetailId == od.OrderDetailId)));

            // Add or Update new/existing details
            foreach (var newOrderDetail in OrderDetails)
            {
                if (newOrderDetail.BookId == 0 || newOrderDetail.Quantity <= 0)
                {
                    // Skip invalid order details
                    continue;
                }

                var existingOrderDetail = orderToUpdate.OrderDetails.FirstOrDefault(od => od.OrderDetailId == newOrderDetail.OrderDetailId);

                if (existingOrderDetail == null)
                {
                    // Add new OrderDetail
                    var book = await _context.Book.FindAsync(newOrderDetail.BookId);
                    if (book != null)
                    {
                        orderToUpdate.OrderDetails.Add(new OrderDetail
                        {
                            OrderId = orderToUpdate.OrderId,
                            BookId = newOrderDetail.BookId,
                            Quantity = newOrderDetail.Quantity,
                            UnitPrice = book.Price // Use current book price
                        });
                    }
                }
                else
                {
                    // Update existing OrderDetail
                    var book = await _context.Book.FindAsync(newOrderDetail.BookId);
                    if (book != null)
                    {
                        existingOrderDetail.BookId = newOrderDetail.BookId;
                        existingOrderDetail.Quantity = newOrderDetail.Quantity;
                        existingOrderDetail.UnitPrice = book.Price; // Update unit price in case book price changed
                    }
                }
            }

            // Recalculate TotalAmount
            decimal subtotal = orderToUpdate.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);
            decimal total = subtotal;

            if (!string.IsNullOrEmpty(orderToUpdate.DiscountCode))
            {
                var discount = await _context.DiscountCode.FindAsync(orderToUpdate.DiscountCode);
                if (discount != null && subtotal >= discount.MinimumOrder)
                {
                    if (discount.DiscountType == "Percent")
                    {
                        total = subtotal * (1 - (decimal)discount.Value / 100);
                    }
                    else // Fixed
                    {
                        total = subtotal - (decimal)discount.Value;
                    }
                }
            }
            orderToUpdate.TotalAmount = (int)Math.Max(0, total); // Ensure total is not negative

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(orderToUpdate.OrderId))
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

            // If ModelState is not valid, re-populate ViewData and return view
            ViewData["DiscountCode"] = new SelectList(_context.DiscountCode, "DiscountCodeId", "DiscountCodeId", orderToUpdate.DiscountCode);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "UserId", orderToUpdate.UserId);

            var books = _context.Book.Select(b => new { b.BookId, b.Title, b.Price }).ToList();
            ViewData["BookListForJs"] = books;
            ViewData["Books"] = new SelectList(books, "BookId", "Title");

            var existingOrderDetailsForJson = orderToUpdate.OrderDetails.Select(od => new
            {
                od.OrderDetailId,
                od.BookId,
                od.Quantity,
                od.UnitPrice,
                Book = new { od.Book.Title }
            }).ToList();
            ViewData["ExistingOrderDetailsJson"] = System.Text.Json.JsonSerializer.Serialize(existingOrderDetailsForJson);

            return View(orderToUpdate);
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
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
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
