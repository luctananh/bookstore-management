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
    public class DiscountCodesController : Controller
    {
        private readonly bookstoreeContext _context;
        private readonly CurrentStoreService _currentStoreService;

        public DiscountCodesController(bookstoreeContext context, CurrentStoreService currentStoreService)
        {
            _context = context;
            _currentStoreService = currentStoreService;
        }

        // GET: DiscountCodes
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index(string searchString, string searchField)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentField"] = searchField;

            var searchFields = new Dictionary<string, string>
            {
                { "DiscountCodeId", "Mã giảm giá" },
                { "Description", "Mô tả" }
            };
            ViewData["SearchFields"] = searchFields;

            var discountCodes = from d in _context.DiscountCode
                                select d;

            if (!String.IsNullOrEmpty(searchString))
            {
                switch (searchField)
                {
                    case "DiscountCodeId":
                        discountCodes = discountCodes.Where(s => s.DiscountCodeId.Contains(searchString));
                        break;
                    case "Description":
                        discountCodes = discountCodes.Where(s => s.Description.Contains(searchString));
                        break;
                    default:
                        discountCodes = discountCodes.Where(s => s.DiscountCodeId.Contains(searchString)
                                                               || s.Description.Contains(searchString));
                        break;
                }
            }

            return View(await discountCodes.AsNoTracking().ToListAsync());
        }

        // GET: DiscountCodes/Details/5
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discountCode = await _context.DiscountCode
                .FirstOrDefaultAsync(m => m.DiscountCodeId == id);
            if (discountCode == null)
            {
                return NotFound();
            }

            return View(discountCode);
        }

        // GET: DiscountCodes/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: DiscountCodes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DiscountCodeId,Description,DiscountType,Value,MinimumOrder,StartDate,EndDate,StoreId")] DiscountCode discountCode)
        {
            if (ModelState.IsValid)
            {
                var currentStoreId = _currentStoreService.GetCurrentStoreId();
                if (!currentStoreId.HasValue)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy ID cửa hàng hiện tại.";
                    return RedirectToAction("AccessDenied", "Home");
                }
                discountCode.StoreId = currentStoreId.Value;

                _context.Add(discountCode);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Mã giảm giá đã được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(discountCode);
        }

        // GET: DiscountCodes/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discountCode = await _context.DiscountCode.FindAsync(id);
            if (discountCode == null)
            {
                return NotFound();
            }
            return View(discountCode);
        }

        // POST: DiscountCodes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("DiscountCodeId,Description,DiscountType,Value,MinimumOrder,StartDate,EndDate,StoreId")] DiscountCode discountCode)
        {
            if (id != discountCode.DiscountCodeId)
            {
                return NotFound();
            }

            var currentStoreId = _currentStoreService.GetCurrentStoreId();
            if (!currentStoreId.HasValue || discountCode.StoreId != currentStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa mã giảm giá này.";
                return RedirectToAction("AccessDenied", "Home");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(discountCode);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Mã giảm giá đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DiscountCodeExists(discountCode.DiscountCodeId))
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
            return View(discountCode);
        }

        // GET: DiscountCodes/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discountCode = await _context.DiscountCode
                .FirstOrDefaultAsync(m => m.DiscountCodeId == id);
            if (discountCode == null)
            {
                return NotFound();
            }

            return View(discountCode);
        }

        // POST: DiscountCodes/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var discountCode = await _context.DiscountCode.FindAsync(id);
            var currentStoreId = _currentStoreService.GetCurrentStoreId();

            if (discountCode == null)
            {
                return NotFound();
            }

            if (!currentStoreId.HasValue || discountCode.StoreId != currentStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa mã giảm giá này.";
                return RedirectToAction("AccessDenied", "Home");
            }

            _context.DiscountCode.Remove(discountCode);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Mã giảm giá đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool DiscountCodeExists(string id)
        {
            return _context.DiscountCode.Any(e => e.DiscountCodeId == id);
        }
    }
}
