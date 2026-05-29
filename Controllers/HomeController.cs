using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using NguyenHoangNhat_Tuan3.Models;
using NguyenHoangNhat_Tuan3.ViewModels;

namespace NguyenHoangNhat_Tuan3.Controllers
{
    public class HomeController : Controller
    {
        private readonly WebBanHangContext _context;

        public HomeController(WebBanHangContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId, string? search, string sort = "popular")
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(category => category.IsActive)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .ToListAsync();

            IQueryable<Product> query = _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Where(product => product.IsActive && product.Category.IsActive);

            if (categoryId.HasValue)
            {
                query = query.Where(product => product.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(product =>
                    product.Name.Contains(keyword) ||
                    (product.Brand != null && product.Brand.Contains(keyword)));
            }

            query = sort switch
            {
                "price-asc" => query.OrderBy(product => product.Price),
                "price-desc" => query.OrderByDescending(product => product.Price),
                "newest" => query.OrderByDescending(product => product.CreatedAt),
                _ => query.OrderByDescending(product => product.Sold)
                    .ThenByDescending(product => product.Rating)
            };

            var viewModel = new StorefrontViewModel
            {
                Categories = categories,
                FeaturedProducts = await _context.Products
                    .AsNoTracking()
                    .Include(product => product.Category)
                    .Where(product => product.IsActive && product.IsFeatured)
                    .OrderByDescending(product => product.Sold)
                    .Take(6)
                    .ToListAsync(),
                Products = await query.ToListAsync(),
                CategoryId = categoryId,
                Search = search,
                Sort = sort
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
