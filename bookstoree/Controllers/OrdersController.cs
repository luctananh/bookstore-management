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

using bookstoree.Services;

namespace bookstoree.Controllers
{
    public class OrdersController : Controller
    {
        private readonly bookstoreeContext _context;
        private readonly CurrentStoreService _currentStoreService;

        public OrdersController(bookstoreeContext context, CurrentStoreService currentStoreService)
        {
            _context = context;
            _currentStoreService = currentStoreService;
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

            IQueryable<Order> orders = _context.Order.Include(o => o.DiscountCode).Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book);

            // The global query filter already ensures orders belong to the current store.
            // This additional filter is for Staff to only see their own orders within their store.
            if (User.IsInRole("Staff"))
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    orders = orders.Where(o => o.UserId == currentUserId.Value);
                }
                else
                {
                    // This should ideally not happen if user is authenticated and has a UserId claim
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
                .Include(o => o.DiscountCode)
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
            var currentStoreId = _currentStoreService.GetCurrentStoreId();
            if (!currentStoreId.HasValue)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ID cửa hàng hiện tại.";
                return RedirectToAction("AccessDenied", "Home");
            }

            var viewModel = new OrderCreateViewModel();
            var discounts = _context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value).ToList();
            var discountList = discounts.Select(d => new { d.DiscountCodeId, d.Description }).ToList();
            discountList.Insert(0, new { DiscountCodeId = "", Description = "None" });
            ViewData["DiscountCode"] = new SelectList(discountList, "DiscountCodeId", "Description");

            ViewData["DiscountDetails"] = System.Text.Json.JsonSerializer.Serialize(
                discounts.ToDictionary(d => d.DiscountCodeId, d => new { d.DiscountType, d.Value, d.MinimumOrder }));

            var books = _context.Book.Where(b => b.StoreId == currentStoreId.Value).Select(b => new { b.BookId, b.Title, b.Price }).ToList();
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
            var currentStoreId = _currentStoreService.GetCurrentStoreId();
            if (!currentStoreId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy ID cửa hàng hiện tại. Vui lòng đăng nhập lại.");
                // Re-populate ViewData and return view
                var discountsForInvalidModel = _context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value).ToList();
                var discountListForInvalidModel = discountsForInvalidModel.Select(d => new { d.DiscountCodeId, d.Description }).ToList();
                discountListForInvalidModel.Insert(0, new { DiscountCodeId = "", Description = "None" });
                ViewData["DiscountCode"] = new SelectList(discountListForInvalidModel, "DiscountCodeId", "Description", viewModel.Order.DiscountCode);

                ViewData["DiscountDetails"] = System.Text.Json.JsonSerializer.Serialize(
                    discountsForInvalidModel.ToDictionary(d => d.DiscountCodeId, d => new { d.DiscountType, d.Value, d.MinimumOrder }));

                var booksForInvalidModel = _context.Book.Where(b => b.StoreId == currentStoreId.Value).Select(b => new { b.BookId, b.Title, b.Price }).ToList();
                ViewData["BookListForJs"] = booksForInvalidModel;
                ViewData["Books"] = new SelectList(booksForInvalidModel, "BookId", "Title");
                ViewData["BookPrices"] = System.Text.Json.JsonSerializer.Serialize(booksForInvalidModel.ToDictionary(b => b.BookId, b => b.Price));
                return View(viewModel);
            }

            if (ModelState.IsValid)
            {
                // Validate that there is at least one order detail
                if (viewModel.OrderDetails == null || !viewModel.OrderDetails.Any(od => od.BookId > 0 && od.Quantity > 0))
                {
                    ModelState.AddModelError("OrderDetails", "Đơn hàng phải có ít nhất một sản phẩm.");
                    // Re-populate ViewData and return view
                    var discounts = _context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value).ToList();
                    var discountList = discounts.Select(d => new { d.DiscountCodeId, d.Description }).ToList();
                    discountList.Insert(0, new { DiscountCodeId = "", Description = "None" });
                    ViewData["DiscountCode"] = new SelectList(discountList, "DiscountCodeId", "Description", viewModel.Order.DiscountCode);

                    ViewData["DiscountDetails"] = System.Text.Json.JsonSerializer.Serialize(
                        discounts.ToDictionary(d => d.DiscountCodeId, d => new { d.DiscountType, d.Value, d.MinimumOrder }));

                    var books = _context.Book.Where(b => b.StoreId == currentStoreId.Value).Select(b => new { b.BookId, b.Title, b.Price }).ToList();
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
                    ViewData["DiscountCode"] = new SelectList(_context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value), "DiscountCodeId", "DiscountCodeId", viewModel.Order.DiscountCode);
                    ViewData["Books"] = new SelectList(_context.Book.Where(b => b.StoreId == currentStoreId.Value), "BookId", "Title");
                    return View(viewModel);
                }

                viewModel.Order.StoreId = currentStoreId.Value; // Assign StoreId to the order

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
                            // Kiểm tra tồn kho
                            if (item.Quantity > book.StockQuantity)
                            {
                                ModelState.AddModelError("", $"Sách '{book.Title}' chỉ còn {book.StockQuantity} cuốn trong kho. Vui lòng nhập số lượng hợp lệ.");
                                
                                // Load lại dữ liệu cho View
                                var discounts = _context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value).ToList();
                                var discountList = discounts.Select(d => new { d.DiscountCodeId, d.Description }).ToList();
                                discountList.Insert(0, new { DiscountCodeId = "", Description = "None" });
                                ViewData["DiscountCode"] = new SelectList(discountList, "DiscountCodeId", "Description", viewModel.Order.DiscountCode);
                                ViewData["DiscountDetails"] = System.Text.Json.JsonSerializer.Serialize(
                                    discounts.ToDictionary(d => d.DiscountCodeId, d => new { d.DiscountType, d.Value, d.MinimumOrder }));

                                var books = _context.Book.Where(b => b.StoreId == currentStoreId.Value).Select(b => new { b.BookId, b.Title, b.Price }).ToList();
                                ViewData["BookListForJs"] = books;
                                ViewData["Books"] = new SelectList(books, "BookId", "Title");
                                ViewData["BookPrices"] = System.Text.Json.JsonSerializer.Serialize(books.ToDictionary(b => b.BookId, b => b.Price));

                                return View(viewModel); // Trả lại form với lỗi
                            }

                            // Trừ số lượng
                            book.StockQuantity -= item.Quantity;

                            _context.Update(book);

                            var orderDetail = new OrderDetail
                            {
                                OrderId = viewModel.Order.OrderId,
                                BookId = item.BookId,
                                Quantity = item.Quantity,
                                UnitPrice = book.Price,
                                StoreId = currentStoreId.Value
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
            var discountsOnInvalidModel = _context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value).ToList();
            var discountListOnInvalidModel = discountsOnInvalidModel.Select(d => new { d.DiscountCodeId, d.Description }).ToList();
            discountListOnInvalidModel.Insert(0, new { DiscountCodeId = "", Description = "None" });
            ViewData["DiscountCode"] = new SelectList(discountListOnInvalidModel, "DiscountCodeId", "Description", viewModel.Order.DiscountCode);
            ViewData["Books"] = new SelectList(_context.Book.Where(b => b.StoreId == currentStoreId.Value), "BookId", "Title");
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

            var currentStoreId = _currentStoreService.GetCurrentStoreId();
            if (!currentStoreId.HasValue || order.StoreId != currentStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa đơn hàng này.";
                return RedirectToAction("AccessDenied", "Home");
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
            ViewData["DiscountCode"] = new SelectList(_context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value), "DiscountCodeId", "DiscountCodeId", order.DiscountCode);
            ViewData["UserId"] = new SelectList(_context.User.Where(u => u.StoreId == currentStoreId.Value), "UserId", "UserId", order.UserId);

            var books = _context.Book.Where(b => b.StoreId == currentStoreId.Value).Select(b => new { b.BookId, b.Title, b.Price }).ToList();
            ViewData["BookListForJs"] = books;
            ViewData["Books"] = new SelectList(books, "BookId", "Title");
            ViewData["BookPrices"] = books.ToDictionary(b => b.BookId, b => b.Price);

            var discounts = _context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value).ToList();
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
                        public async Task<IActionResult> Edit(int id, [Bind("OrderId,UserId,OrderDate,Status,PaymentMethod,AppliedDiscountCode,TotalAmount,DiscountCodeId,StoreId")] Order order, List<OrderDetail> OrderDetails)
                        {
                            if (id != order.OrderId)
                            {
                                return NotFound();
                            }
                
                            var currentStoreId = _currentStoreService.GetCurrentStoreId();
                            if (!currentStoreId.HasValue)
                            {
                                ModelState.AddModelError(string.Empty, "Không tìm thấy ID cửa hàng hiện tại. Vui lòng đăng nhập lại.");
                                // Re-populate ViewData and return view
                                var discountsForInvalidModel = _context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value).ToList();
                                var discountListForInvalidModel = discountsForInvalidModel.Select(d => new { d.DiscountCodeId, d.Description }).ToList();
                                discountListForInvalidModel.Insert(0, new { DiscountCodeId = "", Description = "None" });
                                ViewData["DiscountCode"] = new SelectList(discountListForInvalidModel, "DiscountCodeId", "Description", order.AppliedDiscountCode);
                                ViewData["UserId"] = new SelectList(_context.User.Where(u => u.StoreId == currentStoreId.Value), "UserId", "UserId", order.UserId);
                
                                var booksForInvalidModel = _context.Book.Where(b => b.StoreId == currentStoreId.Value).Select(b => new { b.BookId, b.Title, b.Price }).ToList();
                                ViewData["BookListForJs"] = booksForInvalidModel;
                                ViewData["Books"] = new SelectList(booksForInvalidModel, "BookId", "Title");
                                ViewData["BookPrices"] = booksForInvalidModel.ToDictionary(b => b.BookId, b => b.Price);
                
                                var discountsDetailsForInvalidModel = _context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value).ToList();
                                ViewData["DiscountDetails"] = discountsDetailsForInvalidModel.ToDictionary(d => d.DiscountCodeId, d => new { d.DiscountType, d.Value, d.MinimumOrder });
                
                                var existingOrderDetailsForJson = (OrderDetails ?? new List<OrderDetail>())
                                    .Where(od => od.BookId > 0)
                                    .Select(od => new
                                    {
                                        od.OrderDetailId,
                                        od.BookId,
                                        od.Quantity,
                                        UnitPrice = _context.Book.Find(od.BookId)?.Price ?? 0,
                                        Book = new { Title = _context.Book.Find(od.BookId)?.Title ?? "Không xác định" }
                                    }).ToList();
                                ViewData["ExistingOrderDetailsJson"] = System.Text.Json.JsonSerializer.Serialize(existingOrderDetailsForJson);
                
                                return View(order);
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
                
                            if (orderToUpdate.StoreId != currentStoreId.Value)
                            {
                                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa đơn hàng này.";
                                return RedirectToAction("AccessDenied", "Home");
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
                                orderToUpdate.AppliedDiscountCode = order.AppliedDiscountCode; // Use AppliedDiscountCode
                
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
                                            existingDetail.StoreId = currentStoreId.Value; // Assign StoreId to order detail
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
                                                UnitPrice = book.Price,
                                                StoreId = currentStoreId.Value // Assign StoreId to order detail
                                            });
                                        }
                                    }
                                }
                
                                decimal subtotal = orderToUpdate.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);
                                decimal total = subtotal;
                
                                if (!string.IsNullOrEmpty(orderToUpdate.AppliedDiscountCode))
                                {
                                    var discount = await _context.DiscountCode.FindAsync(orderToUpdate.AppliedDiscountCode);
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
                            orderToUpdate.AppliedDiscountCode = order.AppliedDiscountCode; // Use AppliedDiscountCode
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
                
                            var discounts = _context.DiscountCode.Where(d => d.StoreId == currentStoreId.Value).ToList();
                            var discountList = discounts.Select(d => new { d.DiscountCodeId, d.Description }).ToList();
                            discountList.Insert(0, new { DiscountCodeId = "", Description = "None" });
                            ViewData["DiscountCode"] = new SelectList(discountList, "DiscountCodeId", "Description", orderToUpdate.AppliedDiscountCode);
                            ViewData["UserId"] = new SelectList(_context.User.Where(u => u.StoreId == currentStoreId.Value), "UserId", "UserId", orderToUpdate.UserId);
                
                            var books = _context.Book.Where(b => b.StoreId == currentStoreId.Value).Select(b => new { b.BookId, b.Title, b.Price }).ToList();
                            ViewData["BookListForJs"] = books;
                            ViewData["Books"] = new SelectList(books, "BookId", "Title");
                            ViewData["BookPrices"] = books.ToDictionary(b => b.BookId, b => b.Price);
                
                            ViewData["DiscountDetails"] = discounts.ToDictionary(d => d.DiscountCodeId, d => new { d.DiscountType, d.Value, d.MinimumOrder });
                
                            ViewData["ExistingOrderDetailsJson"] = System.Text.Json.JsonSerializer.Serialize(invalidDetailsForJson);
                
                            return View(orderToUpdate);
                        }        // GET: Orders/Delete/5
        [Authorize] // Allow any authenticated user
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.DiscountCode)
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
            var order = await _context.Order
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            var currentStoreId = _currentStoreService.GetCurrentStoreId();

            if (order == null)
            {
                return NotFound(); // Order not found
            }

            if (!currentStoreId.HasValue || order.StoreId != currentStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa đơn hàng này.";
                return RedirectToAction("AccessDenied", "Home");
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

            // Replenish stock for each book in the order
            foreach (var detail in order.OrderDetails)
            {
                var book = await _context.Book.FindAsync(detail.BookId);
                if (book != null)
                {
                    book.StockQuantity += detail.Quantity;
                    _context.Update(book); // Mark book as modified
                }
            }

            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đơn hàng đã được hoàn thành công!";
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
