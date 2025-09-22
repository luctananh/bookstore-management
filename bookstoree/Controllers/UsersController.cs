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
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using bookstoree.Models.ViewModels;
using bookstoree.Services;
using System.Diagnostics;

namespace bookstoree.Controllers
{
    public class UsersController : Controller
    {
        private readonly bookstoreeContext _context;
        private readonly CurrentStoreService _currentStoreService;

        public UsersController(bookstoreeContext context, CurrentStoreService currentStoreService)
        {
            _context = context;
            _currentStoreService = currentStoreService;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(int storeId, string username, string password)
        {
            Debug.WriteLine($"Login attempt: StoreId={storeId}, Username={username}, Password={password}");

            // First, find the store
            var store = await _context.Store.FirstOrDefaultAsync(s => s.StoreId == storeId);
            if (store == null)
            {
                Debug.WriteLine($"Store with ID {storeId} not found.");
                ModelState.AddModelError(string.Empty, "Mã cửa hàng không tồn tại.");
                return View();
            }
            Debug.WriteLine($"Store found: Name={store.Name}, StoreId={store.StoreId}");

            // Then, find the user within that store
            var user = await _context.User
                                     .IgnoreQueryFilters() // Ignore global filters for this query
                                     .FirstOrDefaultAsync(u => u.UserName == username && u.StoreId == store.StoreId);

            if (user == null)
            {
                Debug.WriteLine($"User {username} not found in store {storeId}.");
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }

            Debug.WriteLine($"User found: UserId={user.UserId}, UserName={user.UserName}, StoredPasswordHash={user.PasswordHash}");
            if (user.PasswordHash != password) // In a real app, hash and verify password
            {
                Debug.WriteLine($"Password mismatch for user {username}. Entered: {password}, Stored: {user.PasswordHash}");
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }

            Debug.WriteLine($"Login successful for user {username} in store {storeId}.");

            // Add StoreId to claims (already done in Register, but ensure it's here too for direct login)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("StoreId", user.StoreId.ToString()) // Ensure StoreId is added
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Keep user logged in across browser sessions
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Example: 7 days expiration
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home"); // Redirect to home or a dashboard
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new StoreRegistrationViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(StoreRegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if admin username or email already exists
                if (await _context.User.AnyAsync(u => u.UserName == model.Username || u.Email == model.Email))
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc Email Admin đã tồn tại.");
                    return View(model);
                }

                // Check if store name already exists
                if (await _context.Store.AnyAsync(s => s.Name == model.StoreName))
                {
                    ModelState.AddModelError(string.Empty, "Tên cửa hàng đã tồn tại.");
                    return View(model);
                }

                // Create new Store
                var store = new Store
                {
                    Name = model.StoreName,
                    Address = model.StoreAddress,
                    PhoneNumber = model.StorePhoneNumber,
                    ContactEmail = model.StoreContactEmail
                };
                _context.Add(store);
                await _context.SaveChangesAsync(); // Save store to get StoreId

                // Create new Admin User for the store
                var adminUser = new User
                {
                    UserName = model.Username,
                    PasswordHash = model.Password, // In a real app, hash this password!
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Role = "Admin", // Explicitly set Admin role for the first user of the store
                    StoreId = store.StoreId // Assign the newly created StoreId
                };
                _context.Add(adminUser);
                await _context.SaveChangesAsync();

                                TempData["SuccessMessage"] = "Đăng ký cửa hàng và tài khoản Admin thành công! Mã shop là: " + store.StoreId;
                                TempData["StoreId"] = store.StoreId; // Pass StoreId to TempData
                
                                // Automatically sign in the new admin user
                                var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, adminUser.UserName),
                                    new Claim(ClaimTypes.NameIdentifier, adminUser.UserId.ToString()),
                                    new Claim(ClaimTypes.Role, adminUser.Role),
                                    new Claim("StoreId", adminUser.StoreId.ToString()) // Add StoreId to claims
                                };
                
                                var claimsIdentity = new ClaimsIdentity(
                                    claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                
                                var authProperties = new AuthenticationProperties
                                {
                                    IsPersistent = true, // Keep user logged in across browser sessions
                                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Example: 7 days expiration
                                };
                
                                await HttpContext.SignInAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme,
                                    new ClaimsPrincipal(claimsIdentity),
                                    authProperties);
                
                                return RedirectToAction("Index", "Home"); // Redirect to home or a dashboard
                            }
                            return View(model);        }

        // GET: Users/CreateUser (Admin only)
        [Authorize(Roles = "Admin")] // Explicitly state for clarity, though controller-level already applies
        public IActionResult CreateUser()
        {
            // Populate roles for dropdown if needed
            ViewBag.Roles = new List<string> { "Admin", "Staff" };
            return View();
        }

        // POST: Users/CreateUser (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")] // Explicitly state for clarity
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([Bind("UserName,PasswordHash,FullName,Email,PhoneNumber,Role,StoreId")] User user)
        {
            if (ModelState.IsValid)
            {
                var currentStoreId = _currentStoreService.GetCurrentStoreId();
                if (!currentStoreId.HasValue)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy ID cửa hàng hiện tại.";
                    return RedirectToAction("AccessDenied", "Home");
                }
                user.StoreId = currentStoreId.Value; // Assign the StoreId here
                Debug.WriteLine($"CreateUser: Assigning StoreId={user.StoreId} to user {user.UserName}");

                // Check if username or email already exists within the current store
                if (await _context.User.AnyAsync(u => u.StoreId == currentStoreId.Value && (u.UserName == user.UserName || u.Email == user.Email)))
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc Email đã tồn tại trong cửa hàng này.");
                    ViewBag.Roles = new List<string> { "Admin", "Staff" };
                    return View(user);
                }

                // Ensure the role is valid (Admin, Staff)
                if (!new List<string> { "Admin", "Staff" }.Contains(user.Role))
                {
                    ModelState.AddModelError(string.Empty, "Vai trò không hợp lệ.");
                    ViewBag.Roles = new List<string> { "Admin", "Staff" };
                    return View(user);
                }

                _context.Add(user);
                Debug.WriteLine($"CreateUser: Attempting to save user {user.UserName} with StoreId {user.StoreId}");
                await _context.SaveChangesAsync();
                Debug.WriteLine($"CreateUser: User {user.UserName} saved successfully with UserId {user.UserId}");
                TempData["SuccessMessage"] = "Người dùng đã được tạo thành công!";
                return RedirectToAction(nameof(Index)); // Redirect to user list after creation
            }
            Debug.WriteLine($"CreateUser: ModelState is invalid. Errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            ViewBag.Roles = new List<string> { "Admin", "Staff" };
            return View(user);
        }

        // GET: Users
        [Authorize(Roles = "Admin")] // Restrict to Admin only
        public async Task<IActionResult> Index(string searchString, string searchField)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentField"] = searchField;

            var searchFields = new Dictionary<string, string>
            {
                { "UserName", "Tên đăng nhập" },
                { "FullName", "Tên đầy đủ" },
                { "Email", "Email" },
                { "Role", "Vai trò" }
            };
            ViewData["SearchFields"] = searchFields;

            var users = from u in _context.User
                        select u;

            if (!String.IsNullOrEmpty(searchString))
            {
                switch (searchField)
                {
                    case "UserName":
                        users = users.Where(s => s.UserName.Contains(searchString));
                        break;
                    case "FullName":
                        users = users.Where(s => s.FullName.Contains(searchString));
                        break;
                    case "Email":
                        users = users.Where(s => s.Email.Contains(searchString));
                        break;
                    case "Role":
                        users = users.Where(s => s.Role.Contains(searchString));
                        break;
                    default:
                        users = users.Where(s => s.UserName.Contains(searchString)
                                               || s.FullName.Contains(searchString)
                                               || s.Email.Contains(searchString)
                                               || s.Role.Contains(searchString));
                        break;
                }
            }

            return View(await users.AsNoTracking().ToListAsync());
        }

        // GET: Users/Details/5 (using ID)
        [Authorize] // Allows any authenticated user to access this action
        public async Task<IActionResult> Details(int? id) // Changed parameter to int? id
        {
            int userIdToView;

            if (id == null)
            {
                // If no ID is provided, assume the user wants to view their own profile
                if (User.Identity.IsAuthenticated)
                {
                    var loggedInUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (loggedInUserIdString == null || !int.TryParse(loggedInUserIdString, out userIdToView))
                    {
                        // Could not get valid UserId from claims
                        return RedirectToAction("AccessDenied", "Home");
                    }
                }
                else
                {
                    // Not authenticated and no ID provided
                    return RedirectToAction("Login", "Users"); // Or AccessDenied
                }
            }
            else
            {
                userIdToView = id.Value;
            }

            var user = await _context.User
                .Include(u => u.Store)
                .FirstOrDefaultAsync(m => m.UserId == userIdToView); // Fetch by UserId
            if (user == null)
            {
                return NotFound();
            }

            // Authorization logic:
            // 1. User is accessing their own profile (compare UserIds from claims and database)
            // 2. User is Admin or Staff (can view any profile)
            if (User.Identity.IsAuthenticated)
            {
                var loggedInUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (loggedInUserIdString != null && int.TryParse(loggedInUserIdString, out int loggedInUserId))
                {
                    if (loggedInUserId == user.UserId) // User is accessing their own profile
                    {
                        return View(user);
                    }
                    else if (User.IsInRole("Admin") || User.IsInRole("Staff")) // Admin/Staff can view any profile
                    {
                        return View(user);
                    }
                }
            }

            // If not authorized to view this profile
            return RedirectToAction("AccessDenied", "Home");
        }

        // GET: Users/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            var currentStoreId = _currentStoreService.GetCurrentStoreId();
            if (!currentStoreId.HasValue)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ID cửa hàng hiện tại.";
                return RedirectToAction("AccessDenied", "Home");
            }
            var user = new User { StoreId = currentStoreId.Value };
            ViewBag.Roles = new List<string> { "Admin", "Staff" }; // Populate roles for dropdown
            return View(user);
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("UserId,UserName,PasswordHash,FullName,Email,PhoneNumber,Role,StoreId")] User user)
        {
            if (ModelState.IsValid)
            {
                var currentStoreId = _currentStoreService.GetCurrentStoreId();
                if (!currentStoreId.HasValue)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy ID cửa hàng hiện tại.";
                    return RedirectToAction("AccessDenied", "Home");
                }
                user.StoreId = currentStoreId.Value;

                // Check if username or email already exists within the current store
                if (await _context.User.AnyAsync(u => u.StoreId == currentStoreId.Value && (u.UserName == user.UserName || u.Email == user.Email)))
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc Email đã tồn tại trong cửa hàng này.");
                    ViewBag.Roles = new List<string> { "Admin", "Staff" };
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,UserName,PasswordHash,FullName,Email,PhoneNumber,Role,StoreId")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            var currentStoreId = _currentStoreService.GetCurrentStoreId();
            if (!currentStoreId.HasValue || user.StoreId != currentStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa người dùng này.";
                return RedirectToAction("AccessDenied", "Home");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Người dùng đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Edit), new { id = user.UserId });
            }
            return View(user);
        }

        // GET: Users/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.User
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.User.FindAsync(id);
            var currentStoreId = _currentStoreService.GetCurrentStoreId();

            if (user == null)
            {
                return NotFound();
            }

            if (!currentStoreId.HasValue || user.StoreId != currentStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa người dùng này.";
                return RedirectToAction("AccessDenied", "Home");
            }

            _context.User.Remove(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Người dùng đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.UserId == id);
        }
    }
}
