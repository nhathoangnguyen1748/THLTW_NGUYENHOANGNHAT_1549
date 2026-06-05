using System;
using System.Collections.Generic;

namespace WebBanHang.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
