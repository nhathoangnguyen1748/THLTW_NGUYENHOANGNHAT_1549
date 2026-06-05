using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Services;
using WebBanHang.ViewModels;

namespace WebBanHang.Controllers;

public class AdminController : Controller
{
    private static readonly string[] OrderStatuses =
    {
        "Chờ xác nhận",
        "Đang giao",
        "Hoàn thành",
        "Đã hủy"
    };

    private readonly WebBanHangContext _context;
    private readonly AdminAuthorizationService _adminAuthorization;
    private readonly IConfiguration _configuration;

    public AdminController(
        WebBanHangContext context,
        AdminAuthorizationService adminAuthorization,
        IConfiguration configuration)
    {
        _context = context;
        _adminAuthorization = adminAuthorization;
        _configuration = configuration;
    }

    public IActionResult Login()
    {
        if (IsSignedIn())
        {
            return RedirectToAction(nameof(Index));
        }

        return View(new AdminLoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var passwordHash = HashPassword(viewModel.Password);
        var admin = await _context.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Username == viewModel.Username.Trim()
                && user.PasswordHash == passwordHash
                && user.IsActive);

        if (admin is null)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng");
            return View(viewModel);
        }

        SetAdminSession(admin.AdminUserId, admin.FullName);

        return RedirectToAction(nameof(Index));
    }

    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]))
        {
            TempData["AuthError"] = "Cần cấu hình Google Client Secret trước khi đăng nhập Google.";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(GoogleResponse), new
        {
            returnUrl = NormalizeLocalReturnUrl(returnUrl)
        });

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        }, GoogleDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            TempData["AuthError"] = "Không thể đăng nhập bằng Google. Vui lòng thử lại.";
            return RedirectToAction(nameof(Login));
        }

        var email = _adminAuthorization.GetEmail(authenticateResult.Principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            ClearAdminSession();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["AuthError"] = "Tài khoản Gmail này chưa có quyền quản trị.";
            return RedirectToAction(nameof(Login));
        }

        var displayName = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name)
            ?? email
            ?? "Google User";

        if (_adminAuthorization.IsAdminEmail(email))
        {
            SetAdminSession(0, displayName, email);
        }
        else
        {
            ClearAdminSession();
        }

        return LocalRedirect(NormalizeLocalReturnUrl(returnUrl) ?? Url.Action("Index", "Home")!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        ClearAdminSession();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Index()
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        var viewModel = new AdminDashboardViewModel
        {
            ProductCount = await _context.Products.CountAsync(),
            OrderCount = await _context.Orders.CountAsync(),
            PendingOrderCount = await _context.Orders.CountAsync(order => order.Status == "Chờ xác nhận"),
            Revenue = await _context.Orders
                .Where(order => order.Status != "Đã hủy")
                .SumAsync(order => (decimal?)order.Total) ?? 0,
            RecentOrders = await _context.Orders
                .AsNoTracking()
                .Include(order => order.OrderItems)
                .OrderByDescending(order => order.CreatedAt)
                .Take(6)
                .ToListAsync(),
            TopProducts = await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .OrderByDescending(product => product.Sold)
                .Take(5)
                .ToListAsync(),
            LowStockProducts = await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Where(product => product.Stock <= 10)
                .OrderBy(product => product.Stock)
                .Take(5)
                .ToListAsync()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Products()
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        var products = await _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .OrderByDescending(product => product.CreatedAt)
            .ToListAsync();

        return View(products);
    }

    public async Task<IActionResult> ProductCreate()
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        return View("ProductForm", new ProductFormViewModel
        {
            Categories = await LoadCategoriesAsync(),
            Rating = 5,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductCreate(ProductFormViewModel viewModel)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        viewModel.Categories = await LoadCategoriesAsync();
        if (!ModelState.IsValid)
        {
            return View("ProductForm", viewModel);
        }

        var product = new Product
        {
            CategoryId = viewModel.CategoryId,
            Name = viewModel.Name.Trim(),
            Slug = await CreateUniqueProductSlugAsync(viewModel.Slug ?? viewModel.Name),
            Description = viewModel.Description?.Trim(),
            Brand = viewModel.Brand?.Trim(),
            Price = viewModel.Price,
            OldPrice = viewModel.OldPrice,
            Stock = viewModel.Stock,
            Sold = viewModel.Sold,
            Rating = viewModel.Rating,
            ImageUrl = NormalizeImageUrl(viewModel.ImageUrl),
            IsFeatured = viewModel.IsFeatured,
            IsActive = viewModel.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        TempData["Toast"] = "Đã thêm sản phẩm mới";

        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> ProductEdit(int id)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return View("ProductForm", new ProductFormViewModel
        {
            ProductId = product.ProductId,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Brand = product.Brand,
            Price = product.Price,
            OldPrice = product.OldPrice,
            Stock = product.Stock,
            Sold = product.Sold,
            Rating = product.Rating,
            ImageUrl = product.ImageUrl,
            IsFeatured = product.IsFeatured,
            IsActive = product.IsActive,
            Categories = await LoadCategoriesAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductEdit(ProductFormViewModel viewModel)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        viewModel.Categories = await LoadCategoriesAsync();
        if (!ModelState.IsValid || viewModel.ProductId is null)
        {
            return View("ProductForm", viewModel);
        }

        var product = await _context.Products.FindAsync(viewModel.ProductId.Value);
        if (product is null)
        {
            return NotFound();
        }

        product.CategoryId = viewModel.CategoryId;
        product.Name = viewModel.Name.Trim();
        product.Slug = await CreateUniqueProductSlugAsync(viewModel.Slug ?? viewModel.Name, product.ProductId);
        product.Description = viewModel.Description?.Trim();
        product.Brand = viewModel.Brand?.Trim();
        product.Price = viewModel.Price;
        product.OldPrice = viewModel.OldPrice;
        product.Stock = viewModel.Stock;
        product.Sold = viewModel.Sold;
        product.Rating = viewModel.Rating;
        product.ImageUrl = NormalizeImageUrl(viewModel.ImageUrl);
        product.IsFeatured = viewModel.IsFeatured;
        product.IsActive = viewModel.IsActive;

        await _context.SaveChangesAsync();
        TempData["Toast"] = "Đã cập nhật sản phẩm";

        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductDelete(int id)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.IsActive = !product.IsActive;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> Categories()
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .Include(category => category.Products)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync();

        return View(categories);
    }

    public IActionResult CategoryCreate()
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        return View("CategoryForm", new CategoryFormViewModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryCreate(CategoryFormViewModel viewModel)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        if (!ModelState.IsValid)
        {
            return View("CategoryForm", viewModel);
        }

        var category = new Category
        {
            Name = viewModel.Name.Trim(),
            Slug = await CreateUniqueCategorySlugAsync(viewModel.Slug ?? viewModel.Name),
            Description = viewModel.Description?.Trim(),
            ImageUrl = NormalizeImageUrl(viewModel.ImageUrl),
            DisplayOrder = viewModel.DisplayOrder,
            IsActive = viewModel.IsActive
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        TempData["Toast"] = "Đã thêm danh mục";

        return RedirectToAction(nameof(Categories));
    }

    public async Task<IActionResult> CategoryEdit(int id)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        var category = await _context.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return View("CategoryForm", new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryEdit(CategoryFormViewModel viewModel)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        if (!ModelState.IsValid || viewModel.CategoryId is null)
        {
            return View("CategoryForm", viewModel);
        }

        var category = await _context.Categories.FindAsync(viewModel.CategoryId.Value);
        if (category is null)
        {
            return NotFound();
        }

        category.Name = viewModel.Name.Trim();
        category.Slug = await CreateUniqueCategorySlugAsync(viewModel.Slug ?? viewModel.Name, category.CategoryId);
        category.Description = viewModel.Description?.Trim();
        category.ImageUrl = NormalizeImageUrl(viewModel.ImageUrl);
        category.DisplayOrder = viewModel.DisplayOrder;
        category.IsActive = viewModel.IsActive;

        await _context.SaveChangesAsync();
        TempData["Toast"] = "Đã cập nhật danh mục";

        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryDelete(int id)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        var category = await _context.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = !category.IsActive;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Categories));
    }

    public async Task<IActionResult> Orders(string? status)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        IQueryable<Order> query = _context.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.CreatedAt);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(order => order.Status == status);
        }

        ViewBag.Status = status;
        ViewBag.Statuses = OrderStatuses;

        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> OrderDetails(int id)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Include(item => item.Customer)
            .Include(item => item.OrderItems)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item => item.OrderId == id);

        if (order is null)
        {
            return NotFound();
        }

        ViewBag.Statuses = OrderStatuses;
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, string status)
    {
        if (GuardAdmin() is { } guard)
        {
            return guard;
        }

        if (!OrderStatuses.Contains(status))
        {
            return BadRequest();
        }

        var order = await _context.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        order.Status = status;
        await _context.SaveChangesAsync();
        TempData["Toast"] = "Đã cập nhật trạng thái đơn hàng";

        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    private bool IsSignedIn()
    {
        return _adminAuthorization.IsAdmin(HttpContext);
    }

    private IActionResult? GuardAdmin()
    {
        return IsSignedIn() ? null : RedirectToAction(nameof(Login));
    }

    private void SetAdminSession(int adminId, string displayName, string? email = null)
    {
        HttpContext.Session.SetInt32(AdminAuthorizationService.AdminIdSessionKey, adminId);
        HttpContext.Session.SetString(AdminAuthorizationService.AdminNameSessionKey, displayName);

        if (!string.IsNullOrWhiteSpace(email))
        {
            HttpContext.Session.SetString(AdminAuthorizationService.AdminEmailSessionKey, email);
        }
    }

    private void ClearAdminSession()
    {
        HttpContext.Session.Remove(AdminAuthorizationService.AdminIdSessionKey);
        HttpContext.Session.Remove(AdminAuthorizationService.AdminNameSessionKey);
        HttpContext.Session.Remove(AdminAuthorizationService.AdminEmailSessionKey);
    }

    private string? NormalizeLocalReturnUrl(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? returnUrl : null;
    }

    private async Task<List<Category>> LoadCategoriesAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync();
    }

    private async Task<string> CreateUniqueProductSlugAsync(string source, int? currentProductId = null)
    {
        return await CreateUniqueSlugAsync(
            source,
            async slug => await _context.Products.AnyAsync(product =>
                product.Slug == slug && (!currentProductId.HasValue || product.ProductId != currentProductId.Value)));
    }

    private async Task<string> CreateUniqueCategorySlugAsync(string source, int? currentCategoryId = null)
    {
        return await CreateUniqueSlugAsync(
            source,
            async slug => await _context.Categories.AnyAsync(category =>
                category.Slug == slug && (!currentCategoryId.HasValue || category.CategoryId != currentCategoryId.Value)));
    }

    private static async Task<string> CreateUniqueSlugAsync(string source, Func<string, Task<bool>> exists)
    {
        var baseSlug = ToSlug(source);
        var slug = baseSlug;
        var index = 2;

        while (await exists(slug))
        {
            slug = $"{baseSlug}-{index}";
            index++;
        }

        return slug;
    }

    private static string ToSlug(string value)
    {
        var normalized = value.Trim()
            .ToLowerInvariant()
            .Replace("đ", "d", StringComparison.Ordinal)
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = builder.ToString().Normalize(NormalizationForm.FormC);
        slug = Regex.Replace(slug, "[^a-z0-9\\s-]", string.Empty);
        slug = Regex.Replace(slug, "[\\s-]+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(slug)
            ? Guid.NewGuid().ToString("N")[..8]
            : slug;
    }

    private static string? NormalizeImageUrl(string? imageUrl)
    {
        return string.IsNullOrWhiteSpace(imageUrl)
            ? null
            : imageUrl.Trim();
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
