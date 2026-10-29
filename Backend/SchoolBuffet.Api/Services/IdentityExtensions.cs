using System.Net;
using System.Security.Claims;

namespace SchoolBuffet.Api.Services;

public static class IdentityExtensions
{
    public static int UserId(this ClaimsPrincipal principal)
    {
        string? raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(raw, out int id) && id > 0)
        {
            return id;
        }

        throw new AppException(HttpStatusCode.Unauthorized, "Sesioni nuk u gjet. Hyresh perseri.");
    }
}
