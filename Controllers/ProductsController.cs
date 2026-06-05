using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.ViewModels;

namespace WebBanHang.Controllers;

public class ProductsController : Controller
{
    private readonly WebBanHangContext _context;

    public ProductsController(WebBanHangContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ProductId == id && item.IsActive);

        if (product is null)
        {
            return NotFound();
        }

        var viewModel = new ProductDetailsViewModel
        {
            Product = product,
            RelatedProducts = await _context.Products
                .AsNoTracking()
                .Include(item => item.Category)
                .Where(item => item.IsActive
                    && item.CategoryId == product.CategoryId
                    && item.ProductId != product.ProductId)
                .OrderByDescending(item => item.Sold)
                .Take(4)
                .ToListAsync()
        };

        return View(viewModel);
    }
}
