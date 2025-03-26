namespace MeercatMonitor;

public class OnlineStatusStore(TestConfig _testConfig)
{
    private const int HistoryLimit = 24 * 60;

    private readonly Dictionary<ToMonitorAddress, List<Result>> _store = [];

    public IEnumerable<Result> GetValues(ToMonitorAddress key)
    {
        if (_testConfig?.FillTestData is not null && !_store.ContainsKey(key))
        {
            for (var i = _testConfig.FillTestData.Value; i > 0; i--)
            {
                var dto = DateTimeOffset.Now.AddMinutes(-10 * i);
                Push(key, new(Status.Online, dto));
            }
        }

        return _store.TryGetValue(key, out var results) ? [.. results] : [];
    }

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
