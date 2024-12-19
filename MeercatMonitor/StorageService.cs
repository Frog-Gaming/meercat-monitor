using Microsoft.Extensions.Logging;

namespace MeercatMonitor;

internal class StorageService(ILogger<StorageService> _log)
{
    public void SaveStatus(string websiteAddress, bool isOnline)
    {
        _log.LogDebug("writing status for {WebsiteAddress}", websiteAddress);
        using var writer = File.AppendText("storage.txt");
        writer.WriteLine($"{websiteAddress.Replace(" ", "%20").Replace("\n", "TODO")} {isOnline} {DateTimeOffset.Now:u}");
    }
}
