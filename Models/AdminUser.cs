using System;
using System.Collections.Generic;

namespace WebBanHang.Models;

public partial class AdminUser
{
    public int AdminUserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
