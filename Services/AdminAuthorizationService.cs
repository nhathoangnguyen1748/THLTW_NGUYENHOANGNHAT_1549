using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace WebBanHang.Services;

public sealed class AdminAuthorizationService
{
    public const string AdminIdSessionKey = "ADMIN_ID";
    public const string AdminNameSessionKey = "ADMIN_NAME";
    public const string AdminEmailSessionKey = "ADMIN_EMAIL";

    private readonly HashSet<string> _adminEmails;

    public AdminAuthorizationService(IConfiguration configuration)
    {
        _adminEmails = configuration
            .GetSection("AdminAuthorization:AdminEmails")
            .GetChildren()
            .Select(section => section.Value)
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => NormalizeEmail(email)!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAdmin(HttpContext httpContext)
    {
        var sessionEmail = httpContext.Session.GetString(AdminEmailSessionKey);
        if (!string.IsNullOrWhiteSpace(sessionEmail))
        {
            return IsAdminEmail(sessionEmail);
        }

        if (httpContext.Session.GetInt32(AdminIdSessionKey).HasValue)
        {
            return true;
        }

        return IsAdminEmail(GetEmail(httpContext.User));
    }

    public bool IsAdminEmail(string? email)
    {
        var normalizedEmail = NormalizeEmail(email);
        return !string.IsNullOrWhiteSpace(normalizedEmail)
            && _adminEmails.Contains(normalizedEmail);
    }

    public string? GetDisplayName(HttpContext httpContext)
    {
        var sessionName = httpContext.Session.GetString(AdminNameSessionKey);
        if (!string.IsNullOrWhiteSpace(sessionName))
        {
            return sessionName;
        }

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return httpContext.User.FindFirstValue(ClaimTypes.Name)
            ?? GetEmail(httpContext.User);
    }

    public string? GetEmail(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email");
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();
    }
}
