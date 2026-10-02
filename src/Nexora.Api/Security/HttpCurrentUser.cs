using System.Security.Claims;

namespace Nexora.Api.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(value, out var userId) && userId != Guid.Empty ? userId : null;
        }
    }
}
