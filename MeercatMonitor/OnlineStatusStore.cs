namespace MeercatMonitor;

public class OnlineStatusStore
{
    public enum Status { Unknown, Online, Offline, }

    public record Result(Status Status, DateTimeOffset Time);

    private const int HistoryLimit = 24 * 60;

    private readonly Dictionary<ToMonitorAddress, List<Result>> _store = [];

    public IEnumerable<Result> GetValues(ToMonitorAddress key) => _store.TryGetValue(key, out var results) ? [.. results] : [];

    public void SetNow(ToMonitorAddress key, Status status) => Push(key, new Result(status, DateTimeOffset.Now));

    private void Push(ToMonitorAddress key, Result result)
    {
        if (!_store.TryGetValue(key, out var list))
        {
            list = [];
            _store[key] = list;
        }

        if (list.Count >= HistoryLimit) list.RemoveAt(0);

        list.Add(result);
    }
}
