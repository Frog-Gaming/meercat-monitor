namespace MeercatMonitor;

public class OnlineStatusStore
{
    public enum Status { Unknown, Online, Offline, }

    public record Result(ToMonitorAddress Target, Status Status, DateTimeOffset Time)
    {
        public static Result Now(ToMonitorAddress target, Status status) => new(target, status, DateTimeOffset.Now);
    }

    private readonly Dictionary<ToMonitorAddress, Result> _store = [];

    public bool TryGetValue(ToMonitorAddress key, out Result? value) => _store.TryGetValue(key, out value);

    public void SetNow(ToMonitorAddress key, Status status) => _store[key] = Result.Now(key, status);

    public IReadOnlyCollection<Result> GetAll() => _store.Values;
}
