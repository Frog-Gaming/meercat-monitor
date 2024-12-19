using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace MeercatMonitor;

internal class OnlineMonitor(Config config, NotificationService _notify, ILogger<OnlineMonitor> _log, StorageService _storageService) : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(config.CheckIntervalS));
    // Distinct() across groups and also work around duplicate config list values
    private readonly string[] _websiteAddresses = config.Monitors.SelectMany(x => x.Addresses).Distinct().ToArray();
    private readonly Dictionary<string, bool> _websiteStatus = [];

    private async Task CheckAddressAsync(string websiteAddress)
    {
        if (websiteAddress.StartsWith("http://") || websiteAddress.StartsWith("https://"))
        {
            await CheckHttpAsync(websiteAddress);
        }
        else if (websiteAddress.StartsWith("ftp://"))
        {
            await CheckFtpAsync(websiteAddress);
        }
        else
        {
            _log.LogWarning("Unknown protocol on {WebsiteAddress}", websiteAddress);
        }
    }

    private async Task CheckHttpAsync(string websiteAddress)
    {
        _log.LogDebug("Checking {WebsiteAddress}…", websiteAddress);

        try
        {
            using HttpClient c = new();

            HttpRequestMessage req = new(HttpMethod.Head, websiteAddress);
            HttpResponseMessage res = await c.SendAsync(req);

            var isOnline = res.IsSuccessStatusCode;

            UpdateStatus(websiteAddress, isOnline);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "HTTP {WebsiteAddress} failed the uptime check with exception {ExceptionMessage}", websiteAddress, ex.Message + ex.InnerException?.Message);

            UpdateStatus(websiteAddress, isOnline: false);
        }
    }

    private async Task CheckFtpAsync(string websiteAddress)
    {
        var (hostname, port) = ParseFtpAddress(websiteAddress);
        _log.LogDebug("Testing FTP (TCP) {Hostname}:{Port}…", hostname, port);

        try
        {
            using TcpClient tcp = new();
            await tcp.ConnectAsync(hostname, port);
            using var stream = tcp.GetStream();
            stream.Close();

            UpdateStatus(websiteAddress, isOnline: true);
        }
        catch (SocketException ex)
        {
            // Socket error codes see https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
            _log.LogWarning(ex, "Failed to connect to ftp (tcp) {Hostname}:{Port}; Exception Message: {Message}, socket error code {ErrorCode}", hostname, port, ex.Message, ex.ErrorCode);

            UpdateStatus(websiteAddress, isOnline: false);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to connect to ftp (tcp) {Hostname}:{Port}; Exception Message: {Message}", hostname, port, ex.Message);

            UpdateStatus(websiteAddress, isOnline: false);
        }

        static (string hostname, int port) ParseFtpAddress(string websiteAddress)
        {
            var parts = websiteAddress["ftp://".Length..].Split(":");
            var hostname = parts[0];
            // We should handle unexpected configured formats here
            var port = int.Parse(parts[1]);

            return (hostname, port);
        }
    }

    private void UpdateStatus(string websiteAddress, bool isOnline)
    {
        _log.LogDebug("{WebsiteAddress} is {Status}", websiteAddress, isOnline ? "online" : "offline");
        _storageService.SaveStatus(websiteAddress, isOnline);

        // Ignore the first visit - we only have online status *change* events
        if (!_websiteStatus.TryGetValue(websiteAddress, out var wasOnline))
        {
            _websiteStatus[websiteAddress] = isOnline;
            return;
        }

        if (wasOnline != isOnline)
        {
            _notify.HandleStatusChange(websiteAddress, isOnline);
        }

        _websiteStatus[websiteAddress] = isOnline;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Starting monitoring of ({MonitorTargetCount}) [{MonitorTargets}]…", _websiteAddresses.Length, string.Join(",", _websiteAddresses));
        do
        {
            _log.LogInformation("Checking {MonitorTargetCount} targets…", _websiteAddresses.Length);
            foreach (var websiteAddress in _websiteAddresses)
            {
                await CheckAddressAsync(websiteAddress);
            }
        }
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested);
        _log.LogDebug("Stopped monitoring");
    }
}
