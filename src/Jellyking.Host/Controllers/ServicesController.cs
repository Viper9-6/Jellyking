using Jellyking.Core.Models;
using Jellyking.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyking.Host.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ServicesController : ControllerBase
{
    private readonly IServiceStore _serviceStore;
    private readonly ICredentialStore _credentialStore;
    private readonly ServiceDetector _detector;

    public ServicesController(
        IServiceStore serviceStore,
        ICredentialStore credentialStore,
        ServiceDetector detector)
    {
        _serviceStore = serviceStore;
        _credentialStore = credentialStore;
        _detector = detector;
    }

    /// <summary>
    /// Returns all enabled services and their current up/down status,
    /// ordered by display priority.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ServiceStatusDto>>(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var statuses = _detector.GetStatuses()
            .Select(ServiceStatusDto.From)
            .ToList();

        return Ok(statuses);
    }

    /// <summary>Returns the current status of a single service by id (slug).</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<ServiceStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(string id)
    {
        var status = _detector.GetStatuses()
            .FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

        if (status is null)
            return NotFound();

        return Ok(ServiceStatusDto.From(status));
    }

    /// <summary>Create a new service. Triggers proxy reload.</summary>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await _serviceStore.SlugExistsAsync(slug))
            return BadRequest(new { message = "A service with that slug already exists." });

        var service = new Service
        {
            Slug = slug,
            Name = request.Name.Trim(),
            Host = request.Host.Trim(),
            Port = request.Port,
            BasePath = NormaliseBasePath(request.BasePath),
            HealthPath = request.HealthPath?.Trim() ?? string.Empty,
            Icon = request.Icon?.Trim() ?? slug,
            WebSocketPaths = request.WebSocketPaths?.Trim() ?? string.Empty,
            Priority = request.Priority,
            Enabled = request.Enabled,
            AuthType = NormaliseAuthType(request.AuthType)
        };

        var created = await _serviceStore.AddAsync(service);

        var cred = BuildCredential(request.Secret, request.Username, request.Password);
        if (cred is not null)
            await _credentialStore.SetCredentialAsync(created.Id, cred);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    /// <summary>Update an existing service. Triggers proxy reload.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _serviceStore.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var newSlug = request.Slug.Trim().ToLowerInvariant();
            if (!string.Equals(newSlug, existing.Slug, StringComparison.OrdinalIgnoreCase) &&
                await _serviceStore.SlugExistsAsync(newSlug))
            {
                return BadRequest(new { message = "A service with that slug already exists." });
            }

            existing.Slug = newSlug;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            existing.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Host))
            existing.Host = request.Host.Trim();
        if (request.Port.HasValue)
            existing.Port = request.Port.Value;
        if (!string.IsNullOrWhiteSpace(request.BasePath))
            existing.BasePath = NormaliseBasePath(request.BasePath);
        if (request.HealthPath is not null)
            existing.HealthPath = request.HealthPath.Trim();
        if (request.Icon is not null)
            existing.Icon = request.Icon.Trim();
        if (request.WebSocketPaths is not null)
            existing.WebSocketPaths = request.WebSocketPaths.Trim();
        if (request.Priority.HasValue)
            existing.Priority = request.Priority.Value;
        if (request.Enabled.HasValue)
            existing.Enabled = request.Enabled.Value;
        if (request.AuthType is not null)
            existing.AuthType = NormaliseAuthType(request.AuthType);

        var updated = await _serviceStore.UpdateAsync(existing);

        // Credential is write-only: leave as-is unless any field is provided.
        if (request.Secret is not null || request.Username is not null || request.Password is not null)
        {
            var existingCred = await _credentialStore.GetCredentialAsync(updated.Id);
            var merged = BuildCredential(
                request.Secret ?? existingCred?.Secret,
                request.Username ?? existingCred?.Username,
                request.Password ?? existingCred?.Password);
            await _credentialStore.SetCredentialAsync(updated.Id, merged);
        }

        return Ok(Map(updated));
    }

    /// <summary>Delete a service. Triggers proxy reload.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _serviceStore.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        await _serviceStore.DeleteAsync(id);
        await _credentialStore.DeleteAsync(id);
        return NoContent();
    }

    private static string NormaliseBasePath(string path)
    {
        var trimmed = path?.Trim() ?? string.Empty;
        if (!trimmed.StartsWith('/'))
            trimmed = "/" + trimmed;
        return trimmed;
    }

    private static string NormaliseAuthType(string? type) => type?.Trim().ToLowerInvariant() switch
    {
        "apikey" => "apikey",
        "jellyfin" => "jellyfin",
        "qbittorrent" => "qbittorrent",
        _ => "none"
    };

    /// <summary>Builds a credential, returning null when every field is empty.</summary>
    private static AuthCredential? BuildCredential(string? secret, string? username, string? password)
    {
        var cred = new AuthCredential
        {
            Secret = string.IsNullOrWhiteSpace(secret) ? null : secret.Trim(),
            Username = string.IsNullOrWhiteSpace(username) ? null : username.Trim(),
            Password = string.IsNullOrWhiteSpace(password) ? null : password.Trim(),
        };
        return cred.IsEmpty ? null : cred;
    }

    private static ServiceDto Map(Service s) => new(
        s.Id,
        s.Slug,
        s.Name,
        s.Host,
        s.Port,
        s.BasePath,
        s.HealthPath,
        s.Icon,
        s.WebSocketPaths,
        s.Priority,
        s.Enabled,
        s.AuthType,
        s.CreatedAt,
        s.UpdatedAt);
}

// -------------------------------------------------------------------------- //
// DTOs — keep the API surface stable and decoupled from internal models.
// -------------------------------------------------------------------------- //

public sealed record ServiceStatusDto(
    string   Id,
    Guid     ServiceId,
    string   Name,
    string   BasePath,
    string   Icon,
    int      Priority,
    string   Host,
    int      Port,
    bool     IsUp,
    string?  DownReason,
    string   AuthType,
    DateTimeOffset LastChecked)
{
    public static ServiceStatusDto From(ServiceStatus s) => new(
        s.Id,
        s.ServiceId,
        s.Name,
        s.BasePath,
        s.Icon,
        s.Priority,
        s.Host,
        s.Port,
        s.IsUp,
        s.DownReason,
        s.AuthType,
        s.LastChecked);
}

public sealed record ServiceDto(
    Guid Id,
    string Slug,
    string Name,
    string Host,
    int Port,
    string BasePath,
    string HealthPath,
    string Icon,
    string WebSocketPaths,
    int Priority,
    bool Enabled,
    string AuthType,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateServiceRequest
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string BasePath { get; set; } = string.Empty;
    public string? HealthPath { get; set; }
    public string? Icon { get; set; }
    public string? WebSocketPaths { get; set; }
    public int Priority { get; set; } = 100;
    public bool Enabled { get; set; } = true;
    public string? AuthType { get; set; }
    /// <summary>Write-only API key / token (apikey, jellyfin). Never returned by GET.</summary>
    public string? Secret { get; set; }
    /// <summary>Write-only username (qbittorrent session login).</summary>
    public string? Username { get; set; }
    /// <summary>Write-only password (qbittorrent session login).</summary>
    public string? Password { get; set; }
}

public sealed record UpdateServiceRequest
{
    public string? Slug { get; set; }
    public string? Name { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? BasePath { get; set; }
    public string? HealthPath { get; set; }
    public string? Icon { get; set; }
    public string? WebSocketPaths { get; set; }
    public int? Priority { get; set; }
    public bool? Enabled { get; set; }
    public string? AuthType { get; set; }
    /// <summary>Write-only API key / token. null = unchanged; empty = clear.</summary>
    public string? Secret { get; set; }
    /// <summary>Write-only username (qbittorrent). null = unchanged.</summary>
    public string? Username { get; set; }
    /// <summary>Write-only password (qbittorrent). null = unchanged.</summary>
    public string? Password { get; set; }
}
