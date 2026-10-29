using System.Security.Claims;

namespace SchoolBuffet.Api.Services;

public static class AppRoles
{
    public const string Admin = "Admin";

    public const string Staff = "Staff";

    public const string Kitchen = "Kitchen";

    public const string Manage = "Admin,Staff";

    public const string Board = "Admin,Staff,Kitchen";

    public static string Label(string? role)
    {
        return role switch
        {
            Admin => "Administrator",
            Staff => "Punetonjes i byfese",
            Kitchen => "Kuzhina",
            _ => "Klient"
        };
    }

    public static bool CanManageOrders(ClaimsPrincipal principal)
    {
        return principal.IsInRole(Admin) || principal.IsInRole(Staff);
    }
}
