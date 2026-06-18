using Microsoft.AspNetCore.Mvc;

namespace Jellyking.Host.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class HealthController : ControllerBase
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;

    private static readonly string Version =
        typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status  = "ok",
        version = Version,
        uptime  = (DateTimeOffset.UtcNow - StartedAt).ToString(@"d\.hh\:mm\:ss"),
    });
}
