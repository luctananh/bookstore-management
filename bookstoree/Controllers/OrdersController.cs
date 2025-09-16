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
                // Validate that there is at least one order detail
                if (viewModel.OrderDetails == null || !viewModel.OrderDetails.Any(od => od.BookId > 0 && od.Quantity > 0))
                {
                    ModelState.AddModelError("OrderDetails", "Đơn hàng phải có ít nhất một sản phẩm.");
                    // Re-populate ViewData and return view
                    var discounts = _context.DiscountCode.ToList();
                    var discountList = discounts.Select(d => new { d.DiscountCodeId, d.Description }).ToList();
                    discountList.Insert(0, new { DiscountCodeId = "", Description = "None" });
                    ViewData["DiscountCode"] = new SelectList(discountList, "DiscountCodeId", "Description", viewModel.Order.DiscountCode);

                    ViewData["DiscountDetails"] = System.Text.Json.JsonSerializer.Serialize(
                        discounts.ToDictionary(d => d.DiscountCodeId, d => new { d.DiscountType, d.Value, d.MinimumOrder }));

                    var books = _context.Book.Select(b => new { b.BookId, b.Title, b.Price }).ToList();
                    ViewData["BookListForJs"] = books;
                    ViewData["Books"] = new SelectList(books, "BookId", "Title");
                    ViewData["BookPrices"] = System.Text.Json.JsonSerializer.Serialize(books.ToDictionary(b => b.BookId, b => b.Price));
                    return View(viewModel);
                }

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
                    ModelState.AddModelError(string.Empty, "Không tìm thấy ID người dùng. Vui lòng đăng nhập.");
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
                TempData["SuccessMessage"] = "Đơn hàng đã được tạo thành công!";
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

            // Validate that there is at least one valid order detail from the form
            if (OrderDetails == null || !OrderDetails.Any(od => od.BookId > 0 && od.Quantity > 0))
            {
                ModelState.AddModelError("OrderDetails", "Đơn hàng phải có ít nhất một sản phẩm.");
            }

            var orderToUpdate = await _context.Order
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (orderToUpdate == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Ownership check for Staff
                if (User.IsInRole("Staff"))
                {
                    var currentUserId = GetCurrentUserId();
                    if (!currentUserId.HasValue || orderToUpdate.UserId != currentUserId.Value)
                    {
                        return RedirectToAction("AccessDenied", "Home");
                    }
                    order.UserId = currentUserId.Value; // Keep the original user ID
                }

                orderToUpdate.OrderDate = order.OrderDate;
                orderToUpdate.Status = order.Status;
                orderToUpdate.PaymentMethod = order.PaymentMethod;
                orderToUpdate.DiscountCode = order.DiscountCode;

                var detailsFromForm = OrderDetails
                    .Where(od => od.BookId > 0 && od.Quantity > 0)
                    .ToList();

                // Remove details that are no longer in the form
                var detailsToRemove = orderToUpdate.OrderDetails
                    .Where(d => !detailsFromForm.Any(formDetail => formDetail.OrderDetailId == d.OrderDetailId))
                    .ToList();
                _context.OrderDetail.RemoveRange(detailsToRemove);

                // Update existing details and add new ones
                foreach (var detailFromForm in detailsFromForm)
                {
                    var existingDetail = orderToUpdate.OrderDetails
                        .FirstOrDefault(d => d.OrderDetailId == detailFromForm.OrderDetailId);

                    if (existingDetail != null)
                    {
                        // Update existing
                        var book = await _context.Book.FindAsync(detailFromForm.BookId);
                        if (book != null)
                        {
                            existingDetail.BookId = detailFromForm.BookId;
                            existingDetail.Quantity = detailFromForm.Quantity;
                            existingDetail.UnitPrice = book.Price;
                        }
                    }
                    else
                    {
                        // Add new
                        var book = await _context.Book.FindAsync(detailFromForm.BookId);
                        if (book != null)
                        {
                            orderToUpdate.OrderDetails.Add(new OrderDetail
                            {
                                OrderId = orderToUpdate.OrderId,
                                BookId = detailFromForm.BookId,
                                Quantity = detailFromForm.Quantity,
                                UnitPrice = book.Price
                            });
                        }
                    }
                }

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
                        else
                        {
                            total = subtotal - (decimal)discount.Value;
                        }
                    }
                }
                orderToUpdate.TotalAmount = (int)Math.Max(0, total);

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đơn hàng đã được cập nhật thành công!";
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
            orderToUpdate.OrderDate = order.OrderDate;
            orderToUpdate.Status = order.Status;
            orderToUpdate.PaymentMethod = order.PaymentMethod;
            orderToUpdate.DiscountCode = order.DiscountCode;
            orderToUpdate.TotalAmount = order.TotalAmount;

            var invalidDetailsForJson = (OrderDetails ?? new List<OrderDetail>())
                .Where(od => od.BookId > 0)
                .Select(od => new {
                    od.OrderDetailId,
                    od.BookId,
                    od.Quantity,
                    UnitPrice = _context.Book.Find(od.BookId)?.Price ?? 0,
                    Book = new { Title = _context.Book.Find(od.BookId)?.Title ?? "Không xác định" }
                }).ToList();

            var discounts = _context.DiscountCode.ToList();
            var discountList = discounts.Select(d => new { d.DiscountCodeId, d.Description }).ToList();
            discountList.Insert(0, new { DiscountCodeId = "", Description = "None" });
            ViewData["DiscountCode"] = new SelectList(discountList, "DiscountCodeId", "Description", orderToUpdate.DiscountCode);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "UserId", orderToUpdate.UserId);

            var books = _context.Book.Select(b => new { b.BookId, b.Title, b.Price }).ToList();
            ViewData["BookListForJs"] = books;
            ViewData["Books"] = new SelectList(books, "BookId", "Title");
            ViewData["BookPrices"] = books.ToDictionary(b => b.BookId, b => b.Price);

            ViewData["DiscountDetails"] = discounts.ToDictionary(d => d.DiscountCodeId, d => new { d.DiscountType, d.Value, d.MinimumOrder });

            ViewData["ExistingOrderDetailsJson"] = System.Text.Json.JsonSerializer.Serialize(invalidDetailsForJson);

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
            TempData["SuccessMessage"] = "Đơn hàng đã được xóa thành công!";
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
