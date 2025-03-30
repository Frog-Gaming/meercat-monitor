namespace MeercatMonitor.Checkers;

public interface IChecker
{
    bool Supports(ToMonitorAddress target);
    Task CheckAsync(ToMonitorAddress target);
}
