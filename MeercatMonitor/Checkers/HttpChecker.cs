using System.Diagnostics;

namespace MeercatMonitor.Checkers
{
    public class HttpChecker(ILogger<HttpChecker> _log, StatusUpdater _statusUpdater) : IChecker
    {
        public bool Supports(ToMonitorAddress target) => target.Address.StartsWith("http://") || target.Address.StartsWith("https://");

        public async Task CheckAsync(ToMonitorAddress target)
        {
            _log.LogDebug("Checking {WebsiteAddress}…", target.Address);

            var sw = Stopwatch.StartNew();
            try
            {
                using HttpClient c = new();
                c.Timeout = TimeSpan.FromSeconds(10);

                HttpRequestMessage req = new(HttpMethod.Head, target.Address);
                HttpResponseMessage res = await c.SendAsync(req);

                var isOnline = res.IsSuccessStatusCode;

                _statusUpdater.UpdateStatus(target, isOnline, sw.Elapsed);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "HTTP {WebsiteAddress} failed the uptime check with exception {ExceptionMessage}", target.Address, ex.Message + ex.InnerException?.Message);

                _statusUpdater.UpdateStatus(target, isOnline: false, sw.Elapsed);
            }
        }
    }
}
