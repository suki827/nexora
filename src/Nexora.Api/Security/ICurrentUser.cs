namespace Nexora.Api.Security;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
