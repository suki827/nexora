using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexora.Api.Contracts;
using Nexora.Api.Security;
using Nexora.Infrastructure.Identity;

namespace Nexora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    IAntiforgery antiforgery,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    public ActionResult<object> GetCsrfToken() =>
        Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<CurrentUserResponse>> Register(
        RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
            return Conflict(new ProblemDetails { Title = "Account already exists", Status = 409 });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName?.Trim(),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(x => x.Description));
        await SignInAsync(user);
        return CreatedAtAction(nameof(Me), new CurrentUserResponse(user.Id, email, user.DisplayName));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<CurrentUserResponse>> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.Status != "active" || user.PasswordHash is null)
            return Unauthorized();
        if (!await userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized();
        if (string.IsNullOrEmpty(user.SecurityStamp))
            await userManager.UpdateSecurityStampAsync(user);
        await SignInAsync(user);
        return Ok(new CurrentUserResponse(user.Id, user.Email!, user.DisplayName));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Unauthorized();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user?.Status != "active")
            return Unauthorized();
        return user is null ? Unauthorized() : Ok(new CurrentUserResponse(user.Id, user.Email!, user.DisplayName));
    }

    private Task SignInAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
                new Claim("nexora:security_stamp", user.SecurityStamp ?? string.Empty)
            ], CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
