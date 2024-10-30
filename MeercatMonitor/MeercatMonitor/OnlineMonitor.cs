namespace MeercatMonitor;

internal class OnlineMonitor(Config config)
{
    public event EventHandler<(string address, bool isOnline)>? WebsiteWentOnline;
    public event EventHandler<(string address, bool isOnline)>? WebsiteWentOffline;

    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(config.CheckIntervalS));
    private readonly string[] _websiteAddresses = config.WebsiteAddress;
    private readonly Dictionary<string, bool> _websiteStatus = [];

    public async Task StartAsync()
    {
        do
        {
            using HttpClient c = new();

            foreach (var websiteAddress in _websiteAddresses)
            {
                HttpResponseMessage res = await c.GetAsync(websiteAddress);
                var isOnline = res.IsSuccessStatusCode;

                if (!_websiteStatus.TryGetValue(websiteAddress, out var wasOnline))
                {
                    _websiteStatus[websiteAddress] = isOnline;
                    continue;
                }

                if (wasOnline != isOnline)
                {
                    OnWebsiteStatusChanged(isOnline, websiteAddress);
                }

                _websiteStatus[websiteAddress] = isOnline;
            }
        }
        while (await _timer.WaitForNextTickAsync());
    }

    private void OnWebsiteStatusChanged(bool isOnline, string websiteAddress)
    {
        if (isOnline)
        {
            WebsiteWentOnline?.Invoke(this, (websiteAddress, isOnline));
        }
        else
        {
            WebsiteWentOffline?.Invoke(this, (websiteAddress, isOnline));
        }
    }
}
