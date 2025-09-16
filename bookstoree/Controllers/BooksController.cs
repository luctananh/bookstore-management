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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Diagnostics;

namespace bookstoree.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly bookstoreeContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BooksController(bookstoreeContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Books
        public async Task<IActionResult> Index(string searchString, string searchField, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentField"] = searchField;

            var searchFields = new Dictionary<string, string>
            {
                { "Title", "Tiêu đề" },
                { "Author", "Tác giả" },
                { "ISBN", "ISBN" },
                { "Price", "Giá" }
            };
            ViewData["SearchFields"] = searchFields;

            var booksQuery = _context.Book.Include(b => b.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                switch (searchField)
                {
                    case "Title":
                        booksQuery = booksQuery.Where(b => b.Title.Contains(searchString));
                        break;
                    case "Author":
                        booksQuery = booksQuery.Where(b => b.Author.Contains(searchString));
                        break;
                    case "ISBN":
                        booksQuery = booksQuery.Where(b => b.ISBN.Contains(searchString));
                        break;
                    case "Price":
                        booksQuery = booksQuery.Where(b => b.Price.ToString().Contains(searchString));
                        break;
                    default: // "All" or empty
                        booksQuery = booksQuery.Where(b =>
                            b.Title.Contains(searchString) ||
                            b.Author.Contains(searchString) ||
                            b.ISBN.Contains(searchString) ||
                            b.Price.ToString().Contains(searchString));
                        break;
                }
            }

            int pageSize = 10;
            var paginatedBooks = await PaginatedList<Book>.CreateAsync(booksQuery.AsNoTracking(), pageNumber ?? 1, pageSize);
            return View(paginatedBooks);
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName");
            return View();
        }

        // POST: Books/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("BookId,ISBN,Title,Description,Author,Publisher,CategoryId,Price,StockQuantity,ImageUrl,DateAdded")] Book book, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Có lỗi khi tạo sản phẩm";
                ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName", book.CategoryId);
                return View(book);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "books");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                book.ImageUrl = "/images/books/" + uniqueFileName;
            }

            _context.Add(book);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm sách thành công!";
            return RedirectToAction("Index");

            TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm sách!";
            return RedirectToAction("Create");
        }


        // GET: Books/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName", book.CategoryId);
            return View(book);
        }

        // POST: Books/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("BookId,ISBN,Title,Description,Author,Publisher,CategoryId,Price,StockQuantity,ImageUrl,DateAdded")] Book book, IFormFile? imageFile)
        {
            if (id != book.BookId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var bookToUpdate = await _context.Book.AsNoTracking().FirstOrDefaultAsync(b => b.BookId == id);
                    if (bookToUpdate == null)
                    {
                        return NotFound();
                    }

                    // Update properties from the form, excluding ImageUrl for now
                    bookToUpdate.ISBN = book.ISBN;
                    bookToUpdate.Title = book.Title;
                    bookToUpdate.Description = book.Description;
                    bookToUpdate.Author = book.Author;
                    bookToUpdate.Publisher = book.Publisher;
                    bookToUpdate.CategoryId = book.CategoryId;
                    bookToUpdate.Price = book.Price;
                    bookToUpdate.StockQuantity = book.StockQuantity;
                    bookToUpdate.DateAdded = book.DateAdded; // Assuming DateAdded can be updated from form

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "books");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        bookToUpdate.ImageUrl = "/images/books/" + uniqueFileName;
                    }
                    else
                    {
                        // Retain the existing ImageUrl from the database
                        bookToUpdate.ImageUrl = bookToUpdate.ImageUrl; 
                    }

                    _context.Update(bookToUpdate); // Now update the tracked entity
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sách đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.BookId))
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
            else // Add this else block
            {
                Debug.WriteLine("--- ModelState is INVALID in BooksController Edit POST ---");
                foreach (var modelStateEntry in ModelState.Values)
                {
                    foreach (var error in modelStateEntry.Errors)
                    {
                        Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }
                Debug.WriteLine("-------------------------------------------------------");
            }
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryId", book.CategoryId);
            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Book.FindAsync(id);
            if (book != null)
            {
                _context.Book.Remove(book);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Sách đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Book.Any(e => e.BookId == id);
        }
    }
}
