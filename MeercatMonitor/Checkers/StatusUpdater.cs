namespace MeercatMonitor.Checkers;

public class StatusUpdater(
    ILogger<StatusUpdater> _log
    , NotificationService _notify
    , OnlineStatusStore _statusStore
    )
{
    public void UpdateStatus(ToMonitorAddress toMonitorAddress, bool isOnline, TimeSpan responseTime)
    {
        _log.LogDebug("{WebsiteAddress} is {Status}", toMonitorAddress.Address, isOnline ? "online" : "offline");

        var newStatus = isOnline ? Status.Online : Status.Offline;

        var prevStates = _statusStore.GetValues(toMonitorAddress);
        // Ignore the first visit - we only have online status *change* events
        if (!prevStates.Any())
        {
            _statusStore.SetNow(toMonitorAddress, newStatus, responseTime);
            return;
        }

        var prevStatus = prevStates.Last().Status;
        if (prevStatus != newStatus)
        {
            _notify.HandleStatusChange(toMonitorAddress, isOnline: newStatus == Status.Online);
        }

        _statusStore.SetNow(toMonitorAddress, newStatus, responseTime);
    }
}
