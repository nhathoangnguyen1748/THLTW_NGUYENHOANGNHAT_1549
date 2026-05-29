using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenHoangNhat_Tuan3.Models;
using NguyenHoangNhat_Tuan3.ViewModels;

namespace NguyenHoangNhat_Tuan3.Controllers;

public class CartController : Controller
{
    public const string SessionKey = "SHOP_CART";

    private readonly WebBanHangContext _context;

    public CartController(WebBanHangContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await BuildCartViewModelAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ProductId == productId && item.IsActive);

        if (product is null)
        {
            return NotFound();
        }

        if (product.Stock <= 0)
        {
            TempData["Toast"] = "Sản phẩm hiện đã hết hàng";
            return RedirectToLocal(returnUrl);
        }

        var cart = ReadCart();
        var existing = cart.FirstOrDefault(item => item.ProductId == productId);
        var safeQuantity = Math.Clamp(quantity, 1, Math.Max(product.Stock, 1));

        if (existing is null)
        {
            cart.Add(new CartSessionItem { ProductId = productId, Quantity = safeQuantity });
        }
        else
        {
            existing.Quantity = Math.Min(existing.Quantity + safeQuantity, Math.Max(product.Stock, 1));
        }

        SaveCart(cart);
        TempData["Toast"] = "Đã thêm sản phẩm vào giỏ hàng";

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ProductId == productId);

        var cart = ReadCart();
        var existing = cart.FirstOrDefault(item => item.ProductId == productId);

        if (existing is not null)
        {
            if (quantity <= 0)
            {
                cart.Remove(existing);
            }
            else
            {
                existing.Quantity = product is null
                    ? Math.Max(1, quantity)
                    : Math.Clamp(quantity, 1, Math.Max(product.Stock, 1));
            }
        }

        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Remove(int productId)
    {
        var cart = ReadCart();
        cart.RemoveAll(item => item.ProductId == productId);
        SaveCart(cart);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Clear()
    {
        SaveCart(new List<CartSessionItem>());
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = await BuildCartViewModelAsync();
        if (!cart.Items.Any())
        {
            TempData["Toast"] = "Giỏ hàng của bạn đang trống";
            return RedirectToAction(nameof(Index));
        }

        return View(new CheckoutViewModel { Cart = cart });
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutViewModel viewModel)
    {
        var cart = await BuildCartViewModelAsync();
        viewModel.Cart = cart;

        if (!cart.Items.Any())
        {
            ModelState.AddModelError(string.Empty, "Giỏ hàng của bạn đang trống");
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var customer = new Customer
        {
            FullName = viewModel.FullName.Trim(),
            Phone = viewModel.Phone.Trim(),
            Email = viewModel.Email?.Trim(),
            Address = viewModel.Address.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            OrderCode = $"NH{DateTime.UtcNow:yyMMddHHmmssfff}",
            CustomerId = customer.CustomerId,
            CustomerName = customer.FullName,
            Phone = customer.Phone,
            Email = customer.Email,
            ShippingAddress = customer.Address,
            Note = viewModel.Note?.Trim(),
            Status = "Chờ xác nhận",
            Subtotal = cart.Subtotal,
            ShippingFee = cart.ShippingFee,
            Total = cart.Total,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in cart.Items)
        {
            var product = await _context.Products.FirstAsync(product => product.ProductId == item.ProductId);
            if (product.Stock < item.Quantity)
            {
                ModelState.AddModelError(string.Empty, $"Sản phẩm {product.Name} chỉ còn {product.Stock} trong kho");
                return View(viewModel);
            }

            product.Stock -= item.Quantity;
            product.Sold += item.Quantity;

            order.OrderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.Name,
                UnitPrice = item.Price,
                Quantity = item.Quantity,
                LineTotal = item.LineTotal
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        SaveCart(new List<CartSessionItem>());
        return RedirectToAction(nameof(Success), new { id = order.OrderId });
    }

    public async Task<IActionResult> Success(int id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(item => item.OrderItems)
            .FirstOrDefaultAsync(item => item.OrderId == id);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    private async Task<CartViewModel> BuildCartViewModelAsync()
    {
        var cart = ReadCart();
        var productIds = cart.Select(item => item.ProductId).ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.ProductId) && product.IsActive)
            .ToListAsync();

        var items = cart
            .Select(item =>
            {
                var product = products.FirstOrDefault(product => product.ProductId == item.ProductId);
                if (product is null)
                {
                    return null;
                }

                return new CartItemViewModel
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    Quantity = Math.Clamp(item.Quantity, 1, Math.Max(product.Stock, 1)),
                    Stock = product.Stock
                };
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();

        return new CartViewModel { Items = items };
    }

    private List<CartSessionItem> ReadCart()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<CartSessionItem>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<CartSessionItem>>(json) ?? new List<CartSessionItem>();
        }
        catch (JsonException)
        {
            return new List<CartSessionItem>();
        }
    }

    private void SaveCart(List<CartSessionItem> cart)
    {
        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(cart));
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
