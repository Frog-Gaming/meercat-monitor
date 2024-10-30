namespace MeercatMonitor;

internal class OnlineMonitor(Config config)
{
    public event EventHandler<string>? WebsiteWentOnline;
    public event EventHandler<string>? WebsiteWentOffline;

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

                var wasOnline = _websiteStatus.TryGetValue(websiteAddress, out var status) ? status : false;

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
            WebsiteWentOnline?.Invoke(this, websiteAddress);
        }
        else
        {
            WebsiteWentOffline?.Invoke(this, websiteAddress);
        }
    }
}
