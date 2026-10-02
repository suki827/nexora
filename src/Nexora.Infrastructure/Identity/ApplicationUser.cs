using Microsoft.AspNetCore.Identity;

namespace Nexora.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? DisplayName { get; set; }

    public string Status { get; set; } = "active";
}
