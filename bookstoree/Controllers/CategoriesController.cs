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

using bookstoree.Services;

namespace bookstoree.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly bookstoreeContext _context;
        private readonly CurrentStoreService _currentStoreService;

        public CategoriesController(bookstoreeContext context, CurrentStoreService currentStoreService)
        {
            _context = context;
            _currentStoreService = currentStoreService;
        }

        // GET: Categories
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index(string searchString, string searchField)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentField"] = searchField;

            var searchFields = new Dictionary<string, string>
            {
                { "Name", "Tên danh mục" }
            };
            ViewData["SearchFields"] = searchFields;

            var categories = from c in _context.Category
                           select c;

            if (!String.IsNullOrEmpty(searchString))
            {
                switch (searchField)
                {
                    case "Name":
                        categories = categories.Where(s => s.Name.Contains(searchString));
                        break;
                    default:
                        categories = categories.Where(s => s.Name.Contains(searchString));
                        break;
                }
            }

            return View(await categories.AsNoTracking().ToListAsync());
        }

        // GET: Categories/Details/5
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Category
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,Name,StoreId")] Category category)
        {
            if (ModelState.IsValid)
            {
                var currentStoreId = _currentStoreService.GetCurrentStoreId();
                if (!currentStoreId.HasValue)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy ID cửa hàng hiện tại.";
                    return RedirectToAction("AccessDenied", "Home");
                }
                category.StoreId = currentStoreId.Value;

                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Danh mục đã được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,StoreId")] Category category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            var currentStoreId = _currentStoreService.GetCurrentStoreId();
            if (!currentStoreId.HasValue || category.StoreId != currentStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa danh mục này.";
                return RedirectToAction("AccessDenied", "Home");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Danh mục đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
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
            return View(category);
        }

        // GET: Categories/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Category
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Category.FindAsync(id);
            var currentStoreId = _currentStoreService.GetCurrentStoreId();

            if (category == null)
            {
                return NotFound();
            }

            if (!currentStoreId.HasValue || category.StoreId != currentStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa danh mục này.";
                return RedirectToAction("AccessDenied", "Home");
            }

            _context.Category.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Danh mục đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Category.Any(e => e.CategoryId == id);
        }
    }
}
