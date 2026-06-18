using System.Net.Sockets;
using Jellyking.Core.Models;
using Jellyking.Core.Proxy;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyking.Core.Services;

/// <summary>
/// Background service that periodically probes every configured service
/// from the persisted <see cref="IServiceStore"/>. When a service's
/// up/down state changes it rebuilds the YARP routing table. Service
/// credentials (for auto-login) are attached from <see cref="ICredentialStore"/>
/// so the proxy can inject them as request headers.
/// </summary>
public sealed class ServiceDetector : BackgroundService
{
    private readonly IServiceStore _serviceStore;
    private readonly ICredentialStore _credentialStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JellykingProxyConfigProvider _proxyConfigProvider;
    private readonly IOptionsSnapshot<AppConfig> _config;
    private readonly ILogger<ServiceDetector> _logger;

    private readonly Dictionary<string, ServiceStatus> _statuses = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _probeLock = new(1, 1);

    public ServiceDetector(
        IServiceStore serviceStore,
        ICredentialStore credentialStore,
        IHttpClientFactory httpClientFactory,
        JellykingProxyConfigProvider proxyConfigProvider,
        IOptionsSnapshot<AppConfig> config,
        ILogger<ServiceDetector> logger)
    {
        _serviceStore = serviceStore;
        _credentialStore = credentialStore;
        _httpClientFactory = httpClientFactory;
        _proxyConfigProvider = proxyConfigProvider;
        _config = config;
        _logger = logger;

        _serviceStore.Changed += OnStoreChanged;
        _credentialStore.Changed += OnStoreChanged;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Service detector running every {Interval}s",
            _config.Value.Detection.IntervalSeconds);

        // Initial probe immediately on startup.
        await ProbeAllAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_config.Value.Detection.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProbeAllAsync(stoppingToken);
        }
    }

    private void OnStoreChanged()
    {
        _logger.LogInformation("Service/credential store changed; triggering immediate probe.");
        _ = Task.Run(async () =>
        {
            try
            {
                await ProbeAllAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Immediate probe after store change failed.");
            }
        });
    }

    private async Task ProbeAllAsync(CancellationToken ct)
    {
        await _probeLock.WaitAsync(ct);
        try
        {
            var services = await _serviceStore.GetAllAsync(ct);
            var enabled = services.Where(s => s.Enabled).ToList();
            var timeout = TimeSpan.FromMilliseconds(_config.Value.Detection.TimeoutMs);

            if (!enabled.Any())
            {
                _statuses.Clear();
                _proxyConfigProvider.Update(Array.Empty<ServiceStatus>());
                return;
            }

            // Load each service's stored secret once this cycle so the proxy
            // can inject it for auto-login.
            var secretTasks = enabled.ToDictionary(
                s => s.Id,
                s => _credentialStore.GetSecretAsync(s.Id, ct));
            await Task.WhenAll(secretTasks.Values);

            var tasks = enabled.Select(s => ProbeServiceAsync(
                s,
                timeout,
                secretTasks[s.Id].GetAwaiter().GetResult(),
                ct));
            var results = await Task.WhenAll(tasks);

            foreach (var status in results)
            {
                var hadStatus = _statuses.TryGetValue(status.Id, out var previous);

                if (!hadStatus || previous!.IsUp != status.IsUp)
                {
                    _logger.LogInformation(
                        "Service {Name} is now {State} (was {PreviousState})",
                        status.Name,
                        status.IsUp ? "UP" : "DOWN",
                        hadStatus ? (previous!.IsUp ? "UP" : "DOWN") : "UNKNOWN");
                }

                _statuses[status.Id] = status;
            }

            // Rebuild the proxy so new/changed services and their auth
            // material (secrets) take effect immediately.
            _proxyConfigProvider.Update(_statuses.Values);
        }
        finally
        {
            _probeLock.Release();
        }
    }

    private async Task<ServiceStatus> ProbeServiceAsync(
        Service service,
        TimeSpan timeout,
        string? secret,
        CancellationToken ct)
    {
        var host = service.Host;
        var port = service.Port;

        // --- Phase 1: TCP probe ---
        try
        {
            using var tcpClient = new TcpClient();
            using var tcpCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            tcpCts.CancelAfter(timeout);

            await tcpClient.ConnectAsync(host, port, tcpCts.Token);
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return Down(service, "port_closed", secret);
        }

        // --- Phase 2: HTTP health check ---
        var healthPath = string.IsNullOrWhiteSpace(service.HealthPath)
            ? "/"
            : service.HealthPath;

        try
        {
            var client = _httpClientFactory.CreateClient("health");
            var url = $"http://{host}:{port}{healthPath}";

            using var httpCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            httpCts.CancelAfter(timeout);

            using var response = await client.GetAsync(url, httpCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return Down(service, $"http_{(int)response.StatusCode}", secret);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "HTTP health check failed for {Name}", service.Name);
            return Down(service, "http_failed", secret);
        }

        return Up(service, secret);
    }

    public IReadOnlyList<ServiceStatus> GetStatuses()
    {
        lock (_statuses)
        {
            return _statuses.Values
                .OrderBy(s => s.Priority)
                .ToList();
        }
    }

    private static ServiceStatus Up(Service service, string? secret) =>
        new()
        {
            Id         = service.Slug,
            ServiceId  = service.Id,
            Name       = service.Name,
            BasePath   = service.BasePath,
            Icon       = service.Icon,
            Priority   = service.Priority,
            Host       = service.Host,
            Port       = service.Port,
            IsUp       = true,
            AuthType   = service.AuthType,
            AuthSecret = secret,
        };

    private static ServiceStatus Down(Service service, string reason, string? secret) =>
        new()
        {
            Id         = service.Slug,
            ServiceId  = service.Id,
            Name       = service.Name,
            BasePath   = service.BasePath,
            Icon       = service.Icon,
            Priority   = service.Priority,
            Host       = service.Host,
            Port       = service.Port,
            IsUp       = false,
            DownReason = reason,
            AuthType   = service.AuthType,
            AuthSecret = secret,
        };
}
