using System.Security.Claims;
using Jellyking.Core.Models;
using Jellyking.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyking.Host.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserStore _userStore;

    public AuthController(IUserStore userStore) => _userStore = userStore;

    /// <summary>Returns true when no admin account exists yet.</summary>
    [HttpGet("setup-required")]
    [AllowAnonymous]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetupRequired()
    {
        var any = await _userStore.AnyAsync();
        return Ok(!any);
    }

    /// <summary>Create the first admin account. Only allowed when no users exist.</summary>
    [HttpPost("setup")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Setup([FromBody] SetupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _userStore.AnyAsync())
            return Conflict(new { message = "Setup already completed." });

        var user = new User
        {
            Username = request.Username.Trim(),
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Role = UserRole.Admin
        };

        await _userStore.AddAsync(user);
        await SignInAsync(user);

        return NoContent();
    }

    /// <summary>Log in an existing user with username and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userStore.GetByUsernameAsync(request.Username.Trim());
        if (user is null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        await SignInAsync(user);
        return NoContent();
    }

    /// <summary>Log out the current user.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>Return the current authenticated user's details.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<MeDto>(StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(idClaim) ||
            !Guid.TryParse(idClaim, out var id) ||
            !Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return Unauthorized();
        }

        return Ok(new MeDto(id, username ?? string.Empty, role));
    }

    /// <summary>Change the calling user's own password.</summary>
    [HttpPost("change-password")]
    [Authorize(Policy = "User")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            return Unauthorized();

        var user = await _userStore.GetByIdAsync(userId);
        if (user is null || !PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Unauthorized(new { message = "Current password is incorrect." });

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { message = "New password must be at least 6 characters." });

        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        await _userStore.UpdateAsync(user);
        return NoContent();
    }

    /// <summary>Cookie auth login redirect target — returns 401 with no redirect.</summary>
    [HttpGet("unauthorized")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult UnauthorizedEndpoint()
    {
        return Unauthorized(new { message = "Authentication required." });
    }

    /// <summary>Cookie auth access-denied redirect target — returns 403 with no redirect.</summary>
    [HttpGet("forbidden")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult ForbiddenEndpoint()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true
            });
    }
}

public sealed record SetupRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record MeDto(Guid Id, string Username, UserRole Role);

public sealed record ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
