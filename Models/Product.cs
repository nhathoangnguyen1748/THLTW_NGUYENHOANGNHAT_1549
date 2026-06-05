using System;
using System.Collections.Generic;

namespace WebBanHang.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? Brand { get; set; }

    public decimal Price { get; set; }

    public decimal? OldPrice { get; set; }

    public int Stock { get; set; }

    public int Sold { get; set; }

    public decimal Rating { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
