using System;
using System.Collections.Generic;

namespace WebBanHang.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public int? CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string? Note { get; set; }

    public string Status { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
