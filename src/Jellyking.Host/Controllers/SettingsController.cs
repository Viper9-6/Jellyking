using Jellyking.Core.Models;
using Jellyking.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyking.Host.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class SettingsController : ControllerBase
{
    private readonly ISettingsStore _settingsStore;

    public SettingsController(ISettingsStore settingsStore) => _settingsStore = settingsStore;

    /// <summary>Current application settings. Any authenticated user can read.</summary>
    [HttpGet]
    [Authorize(Policy = "User")]
    [ProducesResponseType<SettingsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var s = await _settingsStore.GetAsync();
        return Ok(Map(s));
    }

    /// <summary>Update application settings. Admin only.</summary>
    [HttpPut]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<SettingsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request)
    {
        var current = await _settingsStore.GetAsync();
        var updated = new AppSettings
        {
            Title = request.Title ?? current.Title,
            Theme = request.Theme ?? current.Theme,
            LocalAccessEnabled = request.LocalAccessEnabled ?? current.LocalAccessEnabled,
        };

        var saved = await _settingsStore.UpdateAsync(updated);
        return Ok(Map(saved));
    }

    private static SettingsDto Map(AppSettings s) => new(s.Title, s.Theme, s.LocalAccessEnabled);
}

public sealed record SettingsDto(string Title, string Theme, bool LocalAccessEnabled);

public sealed record UpdateSettingsRequest
{
    public string? Title { get; set; }
    public string? Theme { get; set; }
    public bool? LocalAccessEnabled { get; set; }
}
