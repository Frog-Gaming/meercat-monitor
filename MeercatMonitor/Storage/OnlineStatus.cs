namespace MeercatMonitor.Storage;

internal class OnlineStatus
{
    public int OnlineStatusId { get; set; }
    public int TargetId { get; set; }
    public bool Status { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public Target Target { get; set; }
}
