using Jellyking.Core.Models;
using Jellyking.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyking.Host.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "Admin")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserStore _userStore;

    public UsersController(IUserStore userStore) => _userStore = userStore;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userStore.GetAllAsync();
        return Ok(users.Select(u => Map(u)).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userStore.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        return Ok(Map(user));
    }

    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "Username and password are required." });

        var existing = await _userStore.GetByUsernameAsync(request.Username.Trim());
        if (existing is not null)
            return BadRequest(new { message = "Username already exists." });

        var user = new User
        {
            Username = request.Username.Trim(),
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Role = request.Role
        };

        var created = await _userStore.AddAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userStore.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Username))
            user.Username = request.Username.Trim();

        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = PasswordHasher.HashPassword(request.Password);

        user.Role = request.Role ?? user.Role;

        var updated = await _userStore.UpdateAsync(user);
        return Ok(Map(updated));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userStore.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        await _userStore.DeleteAsync(id);
        return NoContent();
    }

    private static UserDto Map(User u) => new(u.Id, u.Username, u.Role, u.CreatedAt, u.UpdatedAt);
}

public sealed record UserDto(Guid Id, string Username, UserRole Role, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
}

public sealed record UpdateUserRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public UserRole? Role { get; set; }
}
