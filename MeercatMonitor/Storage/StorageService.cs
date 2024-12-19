using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeercatMonitor.Storage;

internal class StorageService(ILogger<StorageService> _log)
{
    public void SaveStatus(string websiteAddress, bool isOnline)
    {
        _log.LogInformation("handle status change");
        using var db = new StorageContext();
        db.Database.EnsureCreated();
        var row = db.Target.SingleOrDefault(x => x.Address == websiteAddress);
        if (row is null)
        {
            _log.LogInformation("neuer Eintrag :)");
            row = new Target() { Address = websiteAddress, OnlineStatus = [] };
            db.Target.Add(row);
        }
        _ = db.SaveChanges();
        db.Database.ExecuteSql($"INSERT INTO Target (Status, Timestamp, TargetId) VALUES({isOnline}, {DateTimeOffset.Now}, {row.TargetId})");
        //row.OnlineStatus.Add(new OnlineStatus() { Status = isOnline, Timestamp = DateTimeOffset.Now });
    }
}
