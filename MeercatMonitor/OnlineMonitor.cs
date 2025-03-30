using MeercatMonitor.Checkers;

namespace MeercatMonitor;

internal class OnlineMonitor(Config config
    , ILogger<OnlineMonitor> _log
    , TestConfig _testConfig
    , HttpChecker _httpChecker
    , FtpChecker _ftpChecker
    ) : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(config.CheckIntervalS));
    // Distinct() across groups and also work around duplicate config list values
    private readonly ToMonitorAddress[] _toMonitorAddresses = config.Monitors.SelectMany(x => x.Addresses).Distinct().ToArray();

    private async Task CheckAddressAsync(ToMonitorAddress toMonitorAddress)
    {
        var targetAddress = toMonitorAddress.Address;

        if (_httpChecker.Supports(toMonitorAddress))
        {
            await _httpChecker.CheckAsync(toMonitorAddress);
        }
        else if (_ftpChecker.Supports(toMonitorAddress))
        {
            await _ftpChecker.CheckAsync(toMonitorAddress);
        }
        else
        {
            _log.LogWarning("Unknown protocol on {WebsiteAddress}", targetAddress);
        }
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
