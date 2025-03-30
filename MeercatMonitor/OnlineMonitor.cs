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
    private readonly MonitorTarget[] _monitorTargets = config.Monitors.SelectMany(x => x.Targets).Distinct().ToArray();

    private async Task CheckAsync(MonitorTarget target)
    {
        if (_httpChecker.Supports(target))
        {
            await _httpChecker.CheckAsync(target);
        }
        else if (_ftpChecker.Supports(target))
        {
            await _ftpChecker.CheckAsync(target);
        }
        else
        {
            _log.LogWarning("Unknown protocol on {TargetAddress}", target.Address);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Starting monitoring of ({MonitorTargetCount}) [{MonitorTargets}]…", _monitorTargets.Length, string.Join(", ", _monitorTargets.Select(x => $"{x.Name} ({x.Address})")));
        do
        {
            if (!(_testConfig?.SendMonitorRequests ?? true)) continue;

            _log.LogInformation("Checking {MonitorTargetCount} targets…", _monitorTargets.Length);
            foreach (var target in _monitorTargets)
            {
                await CheckAsync(target);
            }
        }
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested);
        _log.LogDebug("Stopped monitoring");
    }
}
