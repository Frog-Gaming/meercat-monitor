using System.Net.Sockets;

namespace MeercatMonitor;

internal class OnlineMonitor(Config config, NotificationService _notify, ILogger<OnlineMonitor> _log, OnlineStatusStore _statusStore, TestConfig _testConfig) : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(config.CheckIntervalS));
    // Distinct() across groups and also work around duplicate config list values
    private readonly ToMonitorAddress[] _toMonitorAddresses = config.Monitors.SelectMany(x => x.Addresses).Distinct().ToArray();

    private async Task CheckAddressAsync(ToMonitorAddress toMonitorAddress)
    {
        var websiteAddress = toMonitorAddress.Address;

        if (websiteAddress.StartsWith("http://") || websiteAddress.StartsWith("https://"))
        {
            await CheckHttpAsync(toMonitorAddress);
        }
        else if (websiteAddress.StartsWith("ftp://"))
        {
            await CheckFtpAsync(toMonitorAddress);
        }
        else
        {
            _log.LogWarning("Unknown protocol on {WebsiteAddress}", websiteAddress);
        }
    }

    private async Task CheckHttpAsync(ToMonitorAddress toMonitorAddress)
    {
        _log.LogDebug("Checking {WebsiteAddress}…", toMonitorAddress.Address);

        try
        {
            using HttpClient c = new();

            HttpRequestMessage req = new(HttpMethod.Head, toMonitorAddress.Address);
            HttpResponseMessage res = await c.SendAsync(req);

            var isOnline = res.IsSuccessStatusCode;

            UpdateStatus(toMonitorAddress, isOnline);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "HTTP {WebsiteAddress} failed the uptime check with exception {ExceptionMessage}", toMonitorAddress.Address, ex.Message + ex.InnerException?.Message);

            UpdateStatus(toMonitorAddress, isOnline: false);
        }
    }

    private async Task CheckFtpAsync(ToMonitorAddress toMonitorAddress)
    {
        var (hostname, port) = ParseFtpAddress(toMonitorAddress.Address);
        _log.LogDebug("Testing FTP (TCP) {Hostname}:{Port}…", hostname, port);

        try
        {
            using TcpClient tcp = new();
            await tcp.ConnectAsync(hostname, port);
            using var stream = tcp.GetStream();
            stream.Close();

            UpdateStatus(toMonitorAddress, isOnline: true);
        }
        catch (SocketException ex)
        {
            // Socket error codes see https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
            _log.LogWarning(ex, "Failed to connect to ftp (tcp) {Hostname}:{Port}; Exception Message: {Message}, socket error code {ErrorCode}", hostname, port, ex.Message, ex.ErrorCode);

            UpdateStatus(toMonitorAddress, isOnline: false);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to connect to ftp (tcp) {Hostname}:{Port}; Exception Message: {Message}", hostname, port, ex.Message);

            UpdateStatus(toMonitorAddress, isOnline: false);
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

    private void UpdateStatus(ToMonitorAddress toMonitorAddress, bool isOnline)
    {
        _log.LogDebug("{WebsiteAddress} is {Status}", toMonitorAddress.Address, isOnline ? "online" : "offline");

        var newStatus = isOnline ? OnlineStatusStore.Status.Online : OnlineStatusStore.Status.Offline;

        var prevStates = _statusStore.GetValues(toMonitorAddress);
        // Ignore the first visit - we only have online status *change* events
        if (!prevStates.Any())
        {
            _statusStore.SetNow(toMonitorAddress, newStatus);
            return;
        }

        var prevStatus = prevStates.Last().Status;
        if (prevStatus != newStatus)
        {
            _notify.HandleStatusChange(toMonitorAddress, isOnline: newStatus == OnlineStatusStore.Status.Online);
        }

        _statusStore.SetNow(toMonitorAddress, newStatus);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Starting monitoring of ({MonitorTargetCount}) [{MonitorTargets}]…", _toMonitorAddresses.Length, string.Join(", ", _toMonitorAddresses.Select(x => $"{x.Name} ({x.Address})")));
        do
        {
            if (!(_testConfig?.SendMonitorRequests ?? true)) continue;

            _log.LogInformation("Checking {MonitorTargetCount} targets…", _toMonitorAddresses.Length);
            foreach (var websiteAddress in _toMonitorAddresses)
            {
                await CheckAddressAsync(websiteAddress);
            }
        }
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested);
        _log.LogDebug("Stopped monitoring");
    }
}
