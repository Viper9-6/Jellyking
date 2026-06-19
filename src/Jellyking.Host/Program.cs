using System.Security.Claims;
using Jellyking.Core.Models;
using Jellyking.Core.Proxy;
using Jellyking.Core.Services;
using Jellyking.Host.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Microsoft.AspNetCore.DataProtection;
using Yarp.ReverseProxy.Configuration;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Yarp", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine("logs", "jellyking-.log"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Jellyking...");
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var appConfig = builder.Configuration
        .GetSection(AppConfig.SectionName)
        .Get<AppConfig>() ?? new AppConfig();

    // ============================================================
    // Data stores
    // ============================================================
    var dataDirectory = Path.GetFullPath(
        builder.Configuration["DataDirectory"] ??
        Path.Combine(Directory.GetCurrentDirectory(), "data"));
    Directory.CreateDirectory(dataDirectory);
    Log.Information("Using data directory: {DataDirectory}", dataDirectory);

    builder.Services.AddSingleton<IServiceStore, JsonServiceStore>(
        _ => new JsonServiceStore(dataDirectory));
    builder.Services.AddSingleton<IUserStore, JsonUserStore>(
        _ => new JsonUserStore(dataDirectory));

    builder.Services.AddSingleton<ISettingsStore, JsonSettingsStore>(
        _ => new JsonSettingsStore(dataDirectory));

    // Data Protection for encrypting stored credentials at rest.
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDirectory, "keys")));

    builder.Services.AddSingleton<ICredentialStore, JsonCredentialStore>(
        sp => new JsonCredentialStore(dataDirectory, sp.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>()));

    builder.Services.AddSingleton<ISessionAuthenticator, SessionAuthenticator>();

    builder.Services.AddHttpClient("session")
        .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));

    // ============================================================
    // Auth & authorization
    // ============================================================
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/api/v1/auth/unauthorized";
            options.AccessDeniedPath = "/api/v1/auth/forbidden";
            options.Cookie.Name = "jellyking.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = appConfig.Security.UseHttps
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.None;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Admin", policy =>
            policy.RequireClaim(ClaimTypes.Role, nameof(UserRole.Admin)));
        options.AddPolicy("User", policy =>
            policy.RequireAuthenticatedUser());
    });

    // ============================================================
    // HTTP clients
    // ============================================================
    builder.Services.AddHttpClient("health")
        .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(5));

    // ============================================================
    // Core services
    // ============================================================
    builder.Services.AddSingleton<JellykingProxyConfigProvider>();
    builder.Services.AddSingleton<ServiceDetector>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<ServiceDetector>());

    // ============================================================
    // YARP Reverse Proxy
    // ============================================================
    builder.Services.AddSingleton<IProxyConfigProvider>(
        sp => sp.GetRequiredService<JellykingProxyConfigProvider>());
    builder.Services.AddReverseProxy();

    // ============================================================
    // API
    // ============================================================
    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // ============================================================
    // Kestrel / Server
    // ============================================================
    builder.WebHost.ConfigureKestrel(kestrel =>
    {
        var host = System.Net.IPAddress.Parse(appConfig.Server.Host);
        if (appConfig.Security.UseHttps)
        {
            var cert = Jellyking.Host.TlsCertLoader.Load(
                dataDirectory, appConfig.Security.CertPath, appConfig.Security.CertPassword);

            kestrel.Listen(host, appConfig.Security.HttpsPort, lo => lo.UseHttps(cert));
            // Plain HTTP listener purely to redirect to HTTPS.
            kestrel.Listen(host, appConfig.Security.HttpPort);
            Log.Information("HTTPS enabled: https port {Https}, http redirect port {Http}",
                appConfig.Security.HttpsPort, appConfig.Security.HttpPort);
        }
        else
        {
            kestrel.Listen(host, appConfig.Server.Port);
        }
    });

    // ============================================================
    // Build the app
    // ============================================================
    var app = builder.Build();

    if (appConfig.Security.UseHttps)
    {
        if (appConfig.Security.Hsts) app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseSerilogRequestLogging(opts =>
    {
        opts.GetLevel = (ctx, _, _) =>
            ctx.Request.Path.StartsWithSegments("/api")
                ? Serilog.Events.LogEventLevel.Information
                : Serilog.Events.LogEventLevel.Debug;
    });

    // Strip X-Frame-Options / CSP from proxied service responses so the
    // WebUIs can be embedded in Jellyking's in-app iframe view.
    app.UseMiddleware<StripFrameHeadersMiddleware>();

    app.UseMiddleware<BasePathRedirectMiddleware>();

    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseMiddleware<LocalAccessBypassMiddleware>();
    app.UseAuthorization();
    app.UseMiddleware<SessionAuthMiddleware>();

    app.MapControllers();
    app.MapReverseProxy();
    app.MapFallbackToFile("index.html");

    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Jellyking terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
