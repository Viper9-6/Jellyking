using System.Net;
using System.Security.Claims;
using Jellyking.Core.Models;
using Jellyking.Core.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Jellyking.Host.Middleware;

/// <summary>
/// When <see cref="AppSettings.LocalAccessEnabled"/> is true, requests that
/// originate from a loopback (localhost) address and are not already
/// authenticated are given an admin principal for the duration of the
/// request. This lets a single user run Jellyking locally without logging
/// in. Real authenticated sessions always take precedence.
/// </summary>
public sealed class LocalAccessBypassMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISettingsStore _settingsStore;
    private AppSettings _settings;

    public LocalAccessBypassMiddleware(RequestDelegate next, ISettingsStore settingsStore)
    {
        _next = next;
        _settingsStore = settingsStore;
        _settings = _settingsStore.GetAsync().GetAwaiter().GetResult();
        _settingsStore.Changed += () => _settings = _settingsStore.GetAsync().GetAwaiter().GetResult();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only bypass when the toggle is on, the connection is local, and the
        // caller hasn't already authenticated with a real session.
        if (_settings.LocalAccessEnabled &&
            IsLocal(context.Connection) &&
            context.User.Identity?.IsAuthenticated != true)
        {
            var localUserId = new Guid("00000000-0000-0000-0000-000000000001");
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, localUserId.ToString()),
                new(ClaimTypes.Name, "Local"),
                new(ClaimTypes.Role, nameof(UserRole.Admin))
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }

    /// <summary>
    /// Returns true when the request originates from the same machine as
    /// the server (loopback, or remote equals local IP).
    /// </summary>
    private static bool IsLocal(ConnectionInfo conn)
    {
        var remote = conn.RemoteIpAddress;
        if (remote is null)
            return true; // e.g. in-process / unix socket

        if (IPAddress.IsLoopback(remote))
            return true;

        var local = conn.LocalIpAddress;
        return local is not null && remote.Equals(local);
    }
}
