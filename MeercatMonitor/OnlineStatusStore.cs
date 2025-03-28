using System.Text.Json;

namespace MeercatMonitor;

public class OnlineStatusStore
{
    public OnlineStatusStore(TestConfig testConfig, Config config, ILogger<OnlineStatusStore> log)
    {
        _testConfig = testConfig;
        _log = log;

        int? fillTestData = testConfig?.FillTestData;

        var targets = config.Monitors.SelectMany(x => x.Addresses, (x, c) => new { Group = x, Target = c, });
        foreach (var x in targets)
        {
            _mapping.Add(x.Target, x.Group);

            if (fillTestData is not null)
            {
                FillTestData(x.Target, fillTestData.Value);
            }
            else
            {
                LoadDataFromFile(x.Group, x.Target);
            }
        }
    }

    private void FillTestData(ToMonitorAddress target, int fillTestData)
    {
        for (var i = fillTestData; i > 0; i--)
        {
            var status = i % 4 == 0 ? Status.Offline : Status.Online;
            var time = DateTimeOffset.Now.AddMinutes(-10 * i);
            var responseTime = TimeSpan.FromMilliseconds(((i + 1) % 5) * 100);
            Push(target, new(status, time, responseTime));
        }
    }

    private void LoadDataFromFile(MonitorGroup group, ToMonitorAddress target)
    {
        string fileName = GetFileName(group, target);
        if (!File.Exists(fileName)) return;

        var lines = File.ReadAllLines(fileName);
        var lineJson = lines.Select(line => (line, json: JsonSerializer.Deserialize<Result>(line)));

        foreach (var line in lineJson.Where(x => x.json is null).Select(x => x.line))
        {
            _log.LogWarning("Invalid data file {FileName} json line: {Line}", fileName, line);
        }

        var list = lineJson.Select(x => x.json).Where(x => x is not null).Cast<Result>().ToList();

        _store[target] = list;
    }

    private const int HistoryDisplayLimit = 24 * 60;

    private readonly Dictionary<ToMonitorAddress, List<Result>> _store = [];
    private readonly Dictionary<ToMonitorAddress, MonitorGroup> _mapping = [];
    private readonly TestConfig _testConfig;
    private readonly ILogger<OnlineStatusStore> _log;

    public IEnumerable<Result> GetValues(ToMonitorAddress key) => _store.TryGetValue(key, out var results) ? [.. results] : [];

    public void SetNow(ToMonitorAddress key, Status status, TimeSpan responseTime) => Push(key, new Result(status, DateTimeOffset.Now, responseTime));

    private void Push(ToMonitorAddress key, Result result)
    {
        if (!_store.TryGetValue(key, out var list))
        {
            list = [];
            _store[key] = list;
        }

        if (list.Count >= HistoryDisplayLimit) list.RemoveAt(0);

        list.Add(result);

        // During filled data test do not persist anything
        if (_testConfig.FillTestData is not null) return;

        var json = JsonSerializer.Serialize(result);
        var fileName = GetFileName(_mapping[key], key);
        File.AppendAllText(fileName, json + Environment.NewLine);
    }

    private static string GetFileName(MonitorGroup group, ToMonitorAddress target)
    {
        return $"{group.Slug}{target.Slug}.jsonl";
    }
}
