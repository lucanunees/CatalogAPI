using System.Security.Claims;

namespace CatalogAPI.Infrastructure.Auth;

public class UserContext
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub");

        return Guid.Parse(sub!);
    }
}