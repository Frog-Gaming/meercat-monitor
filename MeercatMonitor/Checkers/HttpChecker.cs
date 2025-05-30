using System.Diagnostics;
using System.Net;

namespace MeercatMonitor.Checkers;

public class HttpChecker(ILogger<HttpChecker> _log, StatusUpdater _statusUpdater) : IChecker
{
    public TimeSpan Timeout { get; } = TimeSpan.FromSeconds(10);

    public bool Supports(MonitorTarget target) => target.Address.Scheme is "http" or "https";

    public async Task CheckAsync(MonitorTarget target)
    {
        _log.LogDebug("Checking {TargetAddress}…", target.Address);

        var sw = Stopwatch.StartNew();
        try
        {
            using HttpClient c = new();
            c.Timeout = Timeout;

            HttpRequestMessage req = new(HttpMethod.Head, target.Address);
            HttpResponseMessage res = await c.SendAsync(req);

            var isOnline = res.IsSuccessStatusCode;

            _statusUpdater.UpdateStatus(target, isOnline, sw.Elapsed, FormatStatusCode(res.StatusCode));
        }
        catch (HttpRequestException ex)
        {
            _log.LogWarning(ex, "HTTP {TargetAddress} failed the uptime check with exception {ExceptionMessage}", target.Address, ex.Message + ex.InnerException?.Message);

            _statusUpdater.UpdateStatus(target, isOnline: false, sw.Elapsed, ex.StatusCode is not null ? FormatStatusCode(ex.StatusCode.Value) : $"{ex.Message}; {ex.InnerException?.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _log.LogWarning(ex, "HTTP {TargetAddress} failed the uptime check with exception {ExceptionMessage}", target.Address, ex.Message + ex.InnerException?.Message);

            _statusUpdater.UpdateStatus(target, isOnline: false, sw.Elapsed, "⏱ Timeout");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "HTTP {TargetAddress} failed the uptime check with exception {ExceptionMessage}", target.Address, ex.Message + ex.InnerException?.Message);
        }
    }

    private static string FormatStatusCode(HttpStatusCode statusCode) => $"{(int)statusCode} {statusCode}";
}
