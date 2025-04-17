using System.Text;
using System.Text.Json;

namespace MeercatMonitor;

public class OnlineStatusStore
{
    public Encoding TextEncoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    public int HistoryDisplayLimit { get; }

    public OnlineStatusStore(TestConfig testConfig, Config config, ILogger<OnlineStatusStore> log)
    {
        _testConfig = testConfig;
        _log = log;
        HistoryDisplayLimit = config.HistoryDisplayLimit ?? 24 * 60;

        int? fillTestData = testConfig?.FillTestData;

        var targets = config.Monitors.SelectMany(x => x.Targets, (x, c) => new { Group = x, Target = c, });
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

    private void FillTestData(MonitorTarget target, int fillTestData)
    {
        for (var i = fillTestData; i > 0; i--)
        {
            var status = i % 4 == 0 ? Status.Offline : Status.Online;
            var time = DateTimeOffset.Now.AddMinutes(-10 * i);
            var responseTime = TimeSpan.FromMilliseconds(((i + 1) % 5) * 100);
            var responseDetails = i % 4 == 0 ? "KO" : "OK";
            Push(target, new(status, time, responseTime, responseDetails));
        }
    }

    private void LoadDataFromFile(MonitorGroup group, MonitorTarget target)
    {
        string fileName = GetFileName(group, target);
        if (!File.Exists(fileName)) return;

        var lines = File.ReadAllLines(fileName, TextEncoding).TakeLast(HistoryDisplayLimit);
        var lineJson = lines.Select(line => (line, json: JsonSerializer.Deserialize<Result>(line)));

        foreach (var line in lineJson.Where(x => x.json is null).Select(x => x.line))
        {
            _log.LogWarning("Invalid data file {FileName} json line: {Line}", fileName, line);
        }

        var list = lineJson.Select(x => x.json).Where(x => x is not null).Cast<Result>().ToList();

        _store[target] = list;
    }

    private readonly Dictionary<MonitorTarget, List<Result>> _store = [];
    private readonly Dictionary<MonitorTarget, MonitorGroup> _mapping = [];
    private readonly TestConfig _testConfig;
    private readonly ILogger<OnlineStatusStore> _log;

    public IEnumerable<Result> GetValues(MonitorTarget key) => _store.TryGetValue(key, out var results) ? [.. results] : [];

    public void SetNow(MonitorTarget key, Status status, TimeSpan responseTime, string responseDetails) => Push(key, new Result(status, DateTimeOffset.Now, responseTime, responseDetails));

    private void Push(MonitorTarget key, Result result)
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
        File.AppendAllText(fileName, json + Environment.NewLine, TextEncoding);
    }

    private static string GetFileName(MonitorGroup group, MonitorTarget target)
    {
        return $"{group.Slug}{target.Slug}.jsonl";
    }
}
