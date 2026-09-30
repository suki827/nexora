using Microsoft.AspNetCore.Identity;

namespace Nexora.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }
}