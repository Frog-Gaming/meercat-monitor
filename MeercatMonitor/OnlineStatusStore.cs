using System.Text.Json;

namespace MeercatMonitor;

public class OnlineStatusStore
{
    public OnlineStatusStore(TestConfig testConfig, Config config, ILogger<OnlineStatusStore> _log)
    {
        _testConfig = testConfig;
        _config = config;
        foreach (var monitor in _config.Monitors)
        {
            foreach (var address in monitor.Addresses)
            {
                _mapping.Add(address, monitor);
                string fileName = GetFileName(monitor, address);
                if (File.Exists(fileName))
                {
                    var fileContent = File.ReadAllLines(fileName);
                    List<Result> results = new List<Result>();
                    foreach (var line in fileContent)
                    {
                        Result? result = JsonSerializer.Deserialize<Result>(line);
                        if (result != null)
                        {
                            results.Add(result);
                        }
                        else
                        {
                            _log.LogWarning("Invalid line in {FileName}: {Line}", fileName, line);
                        }
                    }
                    _store[address] = results;
                }
            }
        }
    }
    private const int HistoryLimit = 24 * 60;

    private readonly Dictionary<ToMonitorAddress, List<Result>> _store = [];
    private readonly TestConfig _testConfig;
    private readonly Config _config;
    private readonly Dictionary<ToMonitorAddress, MonitorGroup> _mapping = [];

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
        var json = JsonSerializer.Serialize(result);
        var fileName = GetFileName(_mapping[key], key);
        File.AppendAllText(fileName, json + Environment.NewLine);
    }

    private static string GetFileName(MonitorGroup group, ToMonitorAddress target)
    {
        return $"{group.Slug}{target.Slug}.jsonl";
    }
}
