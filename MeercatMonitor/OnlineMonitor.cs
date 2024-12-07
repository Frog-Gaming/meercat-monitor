using System.Net.Sockets;

namespace MeercatMonitor;

internal class OnlineMonitor(Config config)
{
    public event EventHandler<(string address, bool isOnline)>? WebsiteWentOnline;
    public event EventHandler<(string address, bool isOnline)>? WebsiteWentOffline;

    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(config.CheckIntervalS));
    // Distinct() across groups and also work around duplicate config list values
    private readonly string[] _websiteAddresses = config.Monitors.SelectMany(x => x.Addresses).Distinct().ToArray();
    private readonly Dictionary<string, bool> _websiteStatus = [];

    public async Task StartAsync()
    {
        Log.Information("Starting monitoring of ({MonitorTargetCount}) [{MonitorTargets}]…", _websiteAddresses.Length, string.Join(",", _websiteAddresses));
        do
        {
            Log.Information("Checking {MonitorTargetCount} targets…", _websiteAddresses.Length);
            foreach (var websiteAddress in _websiteAddresses)
            {
                await CheckAddressAsync(websiteAddress);
            }
        }
        while (await _timer.WaitForNextTickAsync());
    }

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
            Log.Warning("Unknown protocol on {WebsiteAddress}", websiteAddress);
        }
    }

    private async Task CheckHttpAsync(string websiteAddress)
    {
        Log.Debug("Checking {WebsiteAddress}…", websiteAddress);

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
            Log.Warning(ex, ex.Message);

            UpdateStatus(websiteAddress, isOnline: false);
        }
    }

    private async Task CheckFtpAsync(string websiteAddress)
    {
        var (hostname, port) = ParseFtpAddress(websiteAddress);
        Log.Debug("Testing FTP (TCP) {Hostname}:{Port}…", hostname, port);

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
            Log.Warning(ex, "Failed to connect to ftp (tcp) {Hostname}:{Port}; Exception Message: {Message}, socket error code {ErrorCode}", hostname, port, ex.Message, ex.ErrorCode);

            UpdateStatus(websiteAddress, isOnline: false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to connect to ftp (tcp) {Hostname}:{Port}; Exception Message: {Message}", hostname, port, ex.Message);

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
        Log.Debug("{WebsiteAddress} is {Status}", websiteAddress, isOnline ? "online" : "offline");

        // Ignore the first visit - we only have online status *change* events
        if (!_websiteStatus.TryGetValue(websiteAddress, out var wasOnline))
        {
            _websiteStatus[websiteAddress] = isOnline;
            return;
        }

        if (wasOnline != isOnline)
        {
            OnWebsiteStatusChanged(isOnline, websiteAddress);
        }

        _websiteStatus[websiteAddress] = isOnline;
    }

    private void OnWebsiteStatusChanged(bool isOnline, string websiteAddress)
    {
        if (isOnline)
        {
            WebsiteWentOnline?.Invoke(this, (websiteAddress, isOnline));
        }
        else
        {
            WebsiteWentOffline?.Invoke(this, (websiteAddress, isOnline));
        }
    }
}
