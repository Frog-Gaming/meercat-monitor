namespace MeercatMonitor.Checkers;

public interface IChecker
{
    bool Supports(MonitorTarget target);
    Task CheckAsync(MonitorTarget target);
}
