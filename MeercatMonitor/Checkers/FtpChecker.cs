using System.Diagnostics;
using System.Net.Sockets;

namespace MeercatMonitor.Checkers;

public class FtpChecker(ILogger<FtpChecker> _log, StatusUpdater _statusUpdater) : IChecker
{
    public static TimeSpan Timeout { get; } = TimeSpan.FromSeconds(10);
    public static bool OpenStream { get; } = false;

    public bool Supports(MonitorTarget target) => target.Address.Scheme is "ftp";

    public async Task CheckAsync(MonitorTarget target)
    {
        var hostname = target.Address.Host;
        var port = target.Address.Port;
        _log.LogDebug("Testing FTP (TCP) {Hostname}:{Port}…", hostname, port);

        var sw = Stopwatch.StartNew();
        try
        {
            using TcpClient tcp = new();
            tcp.SendTimeout = (int)Timeout.TotalMilliseconds;
            tcp.ReceiveTimeout = (int)Timeout.TotalMilliseconds;
            var connected = await WithTimeoutAsync(Timeout, (cToken) => tcp.ConnectAsync(hostname, port, cToken));
            if (!connected)
            {
                _log.LogDebug("Failed to connect; Timeout {Timeout} s", Timeout.TotalSeconds);
                return;
            }

            if (OpenStream)
            {
                using var stream = tcp.GetStream();
                stream.Close();
            }

            _statusUpdater.UpdateStatus(target, isOnline: true, sw.Elapsed);
        }
        catch (SocketException ex)
        {
            // Socket error codes see https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
            _log.LogWarning(ex, "Failed to connect to ftp (tcp) {Hostname}:{Port}; Exception Message: {Message}, socket error code {ErrorCode}", hostname, port, ex.Message, ex.ErrorCode);

            _statusUpdater.UpdateStatus(target, isOnline: false, sw.Elapsed);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to connect to ftp (tcp) {Hostname}:{Port}; Exception Message: {Message}", hostname, port, ex.Message);

            _statusUpdater.UpdateStatus(target, isOnline: false, sw.Elapsed);
        }
    }

    private static async Task<bool> WithTimeoutAsync(TimeSpan timeout, Func<CancellationToken, ValueTask> fn)
    {
        using var cts = new CancellationTokenSource();
        var task = fn(cts.Token).AsTask();
        var timeoutTask = Task.Delay(timeout, cts.Token);
        var r = await Task.WhenAny(task, timeoutTask);
        await cts.CancelAsync();

        return r == task;
    }
}
