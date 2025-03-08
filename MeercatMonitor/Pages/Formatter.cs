namespace MeercatMonitor.Pages
{
    public static class Formatter
    {
        public static string Format(OnlineStatusStore.Status status) => status switch
        {
            OnlineStatusStore.Status.Unknown => "(?)",
            OnlineStatusStore.Status.Online => "✅ online",
            OnlineStatusStore.Status.Offline => "🛑 offline",
            _ => throw new NotImplementedException(),
        };
    }
}
