namespace MeercatMonitor.Checkers;

public class StatusUpdater(
    ILogger<StatusUpdater> _log
    , NotificationService _notify
    , OnlineStatusStore _statusStore
    )
{
    public void UpdateStatus(MonitorTarget target, bool isOnline, TimeSpan responseTime, string responseDetails)
    {
        _log.LogDebug("{TargetAddress} is {Status}", target.Address, isOnline ? "online" : "offline");

        var newStatus = isOnline ? Status.Online : Status.Offline;

        var prevStates = _statusStore.GetValues(target);
        // Ignore the first visit - we only have online status *change* events
        if (!prevStates.Any())
        {
            _statusStore.SetNow(target, newStatus, responseTime, responseDetails);
            return;
        }

        var prevStatus = prevStates.Last().Status;
        if (prevStatus != newStatus)
        {
            _notify.HandleStatusChange(target, isOnline: newStatus == Status.Online);
        }

        _statusStore.SetNow(target, newStatus, responseTime, responseDetails);
    }
}
