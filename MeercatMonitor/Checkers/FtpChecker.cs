using System.Diagnostics;
using System.Net.Sockets;

namespace MeercatMonitor.Checkers;

public class FtpChecker(ILogger<FtpChecker> _log, StatusUpdater _statusUpdater) : IChecker
{
    public bool Supports(ToMonitorAddress target) => target.Address.StartsWith("ftp://");

    public async Task CheckAsync(ToMonitorAddress target)
    {
        if (!Uri.TryCreate(target.Address, UriKind.Absolute, out var uri))
        {
            _log.LogWarning("Invalid FTP address will be ignored; must be a valid Uri but is `{TargetAddress}`", target.Address);
            return;
        }

        var hostname = uri.Host;
        var port = uri.Port;
        _log.LogDebug("Testing FTP (TCP) {Hostname}:{Port}…", hostname, port);

        var sw = Stopwatch.StartNew();
        try
        {
            using TcpClient tcp = new();
            tcp.SendTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
            tcp.ReceiveTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
            await tcp.ConnectAsync(hostname, port);
            using var stream = tcp.GetStream();
            stream.Close();

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
}
