namespace MeercatMonitor.Storage;

internal class Target
{
    public int TargetId { get; set; }
    public string Address { get; set; } = "";

    public List<OnlineStatus> OnlineStatus { get; set; }
}
