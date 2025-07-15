using System.Diagnostics;
using System.Net;

namespace MeercatMonitor.Checkers;

public class HttpChecker(ILogger<HttpChecker> _log, StatusUpdater _statusUpdater, IHttpClientFactory _httpFact, Config _config) : IChecker
{
    public TimeSpan Timeout { get; } = TimeSpan.FromSeconds(10);

    public bool Supports(MonitorTarget target) => target.Address.Scheme is "http" or "https";

    public async Task CheckAsync(MonitorTarget target)
    {
        var result = await CheckAsyncImpl(target);
        // Unhandled unexpected program behavior; warning expected already logged => no further actions
        if (result is null) return;

        if (!result.IsOnline && _config.Retry.SecondChance)
        {
            _log.LogInformation("Determined offline. Starting second-chance check…");
            result = await CheckAsyncImpl(target);
            // Unhandled unexpected program behavior; warning expected already logged => no further actions
            if (result is null) return;
        }

        _statusUpdater.UpdateStatus(result.Target, result.IsOnline, result.Elapsed, result.ResponseDetails);
    }

    private sealed record Result(MonitorTarget Target, bool IsOnline, TimeSpan Elapsed, string ResponseDetails);

    private async Task<Result?> CheckAsyncImpl(MonitorTarget target)
    {
        _log.LogDebug("Checking {TargetAddress}…", target.Address);

        var sw = Stopwatch.StartNew();
        try
        {
            using var c = _httpFact.CreateClient();
            c.Timeout = Timeout;

            HttpRequestMessage req = new(HttpMethod.Head, target.Address);
            HttpResponseMessage res = await c.SendAsync(req);

            var isOnline = res.IsSuccessStatusCode;
            var responseDetails = FormatStatusCode(res.StatusCode);
            return new(target, isOnline, sw.Elapsed, responseDetails);
        }
        catch (HttpRequestException ex)
        {
            _log.LogWarning(ex, "HTTP {TargetAddress} failed the uptime check with exception {ExceptionMessage}", target.Address, ex.Message + ex.InnerException?.Message);
            var isOnline = false;
            var responseDetails = ex.StatusCode is not null ? FormatStatusCode(ex.StatusCode.Value) : $"{ex.Message}; {ex.InnerException?.Message}";
            return new(target, isOnline, sw.Elapsed, responseDetails);
        }
        catch (TaskCanceledException ex)
        {
            _log.LogWarning(ex, "HTTP {TargetAddress} failed the uptime check with exception {ExceptionMessage}", target.Address, ex.Message + ex.InnerException?.Message);
            var isOnline = false;
            var responseDetails = "⏱ Timeout";
            return new(target, isOnline, sw.Elapsed, responseDetails);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "HTTP {TargetAddress} failed the uptime check with exception {ExceptionMessage}", target.Address, ex.Message + ex.InnerException?.Message);
            return null;
        }
    }

    private static string FormatStatusCode(HttpStatusCode statusCode) => $"{(int)statusCode} {statusCode}";
}
