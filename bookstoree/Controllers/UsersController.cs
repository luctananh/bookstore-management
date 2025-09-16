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

namespace bookstoree.Controllers
{
    public class UsersController : Controller
    {
        private readonly bookstoreeContext _context;

        public UsersController(bookstoreeContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null || user.PasswordHash != password) // In a real app, hash and verify password
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

            var authProperties = new AuthenticationProperties
            {
                // AllowRefresh = true,
                // ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                // IsPersistent = true,
                // IssuedUtc = DateTimeOffset.UtcNow,
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
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("UserName,PasswordHash,FullName,Email,PhoneNumber,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                // Check if username or email already exists
                if (await _context.User.AnyAsync(u => u.UserName == user.UserName || u.Email == user.Email))
                {
                    ModelState.AddModelError(string.Empty, "Username or Email already exists.");
                    return View(user);
                }

                // Set default role to 'Customer' if not provided or if it's not 'Admin' or 'Staff'
                if (string.IsNullOrEmpty(user.Role) || (user.Role != "Admin" && user.Role != "Staff"))
                {
                    user.Role = "Customer";
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công!";

                // Automatically sign in the new user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Role, user.Role),
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

                var authProperties = new AuthenticationProperties();

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }
            return View(user);
        }

        // GET: Users/CreateUser (Admin only)
        [Authorize(Roles = "Admin")] // Explicitly state for clarity, though controller-level already applies
        public IActionResult CreateUser()
        {
            // Populate roles for dropdown if needed
            ViewBag.Roles = new List<string> { "Admin", "Staff", "Customer" };
            return View();
        }

        // POST: Users/CreateUser (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")] // Explicitly state for clarity
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([Bind("UserName,PasswordHash,FullName,Email,PhoneNumber,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                // Check if username or email already exists
                if (await _context.User.AnyAsync(u => u.UserName == user.UserName || u.Email == user.Email))
                {
                    ModelState.AddModelError(string.Empty, "Username or Email already exists.");
                    ViewBag.Roles = new List<string> { "Admin", "Staff", "Customer" };
                    return View(user);
                }

                // Ensure the role is valid (Admin, Staff, Customer)
                if (!new List<string> { "Admin", "Staff", "Customer" }.Contains(user.Role))
                {
                    ModelState.AddModelError(string.Empty, "Invalid role specified.");
                    ViewBag.Roles = new List<string> { "Admin", "Staff", "Customer" };
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Người dùng đã được tạo thành công!";
                return RedirectToAction(nameof(Index)); // Redirect to user list after creation
            }
            ViewBag.Roles = new List<string> { "Admin", "Staff", "Customer" };
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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,UserName,PasswordHash,FullName,Email,PhoneNumber,Role")] User user)
        {
            if (ModelState.IsValid)
            {
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
        public async Task<IActionResult> Edit(int id, [Bind("UserId,UserName,PasswordHash,FullName,Email,PhoneNumber,Role")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
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
            if (user != null)
            {
                _context.User.Remove(user);
            }

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
