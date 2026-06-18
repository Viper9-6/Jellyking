using Jellyking.Core.Models;
using Jellyking.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyking.Host.Controllers;

/// <summary>
/// Returns pre-filled service templates used by the "Add service" modal
/// so the admin can start from known defaults (Jellyfin, Sonarr, …).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class TemplatesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ServiceDto>>(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var dtos = ServiceTemplates.All
            .Select(t => new ServiceDto(
                Id: Guid.Empty,            // templates have no persisted id
                Slug: t.Slug,
                Name: t.Name,
                Host: t.Host,
                Port: t.Port,
                BasePath: t.BasePath,
                HealthPath: t.HealthPath,
                Icon: t.Icon,
                WebSocketPaths: t.WebSocketPaths,
                Priority: t.Priority,
                Enabled: t.Enabled,
                AuthType: "none",
                CreatedAt: DateTimeOffset.UtcNow,
                UpdatedAt: DateTimeOffset.UtcNow))
            .ToList();

        return Ok(dtos);
    }
}
