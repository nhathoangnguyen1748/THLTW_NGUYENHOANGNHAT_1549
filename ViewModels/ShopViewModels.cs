using System.ComponentModel.DataAnnotations;
using NguyenHoangNhat_Tuan3.Models;

namespace NguyenHoangNhat_Tuan3.ViewModels;

public class StorefrontViewModel
{
    public List<Category> Categories { get; set; } = new();

    public List<Product> Products { get; set; } = new();

    public List<Product> FeaturedProducts { get; set; } = new();

    public int? CategoryId { get; set; }

    public string? Search { get; set; }

    public string Sort { get; set; } = "popular";
}

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = null!;

    public List<Product> RelatedProducts { get; set; } = new();
}

public class CartSessionItem
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}

public class CartItemViewModel
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int Stock { get; set; }

    public decimal LineTotal => Price * Quantity;
}

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();

    public decimal Subtotal => Items.Sum(item => item.LineTotal);

    public decimal ShippingFee => Items.Count == 0 || Subtotal >= 1_000_000 ? 0 : 25_000;

    public decimal Total => Subtotal + ShippingFee;

    public int TotalQuantity => Items.Sum(item => item.Quantity);
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(140)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(160)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }

    public CartViewModel Cart { get; set; } = new();
}

public class AdminLoginViewModel
{
    [Required(ErrorMessage = "Nhập tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class AdminDashboardViewModel
{
    public int ProductCount { get; set; }

    public int OrderCount { get; set; }

    public int PendingOrderCount { get; set; }

    public decimal Revenue { get; set; }

    public List<Order> RecentOrders { get; set; } = new();

    public List<Product> TopProducts { get; set; } = new();

    public List<Product> LowStockProducts { get; set; } = new();
}

public class ProductFormViewModel
{
    public int? ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn danh mục")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Nhập tên sản phẩm")]
    [StringLength(180)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Slug { get; set; }

    public string? Description { get; set; }

    [StringLength(120)]
    public string? Brand { get; set; }

    [Range(1, 999_999_999, ErrorMessage = "Giá phải lớn hơn 0")]
    public decimal Price { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Giá cũ không hợp lệ")]
    public decimal? OldPrice { get; set; }

    [Range(0, 100_000, ErrorMessage = "Tồn kho không hợp lệ")]
    public int Stock { get; set; }

    [Range(0, 100_000, ErrorMessage = "Đã bán không hợp lệ")]
    public int Sold { get; set; }

    [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5")]
    public decimal Rating { get; set; } = 5;

    [StringLength(600)]
    public string? ImageUrl { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public List<Category> Categories { get; set; } = new();
}

public class CategoryFormViewModel
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Nhập tên danh mục")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(140)]
    public string? Slug { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    [StringLength(600)]
    public string? ImageUrl { get; set; }

    [Range(0, 999)]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
