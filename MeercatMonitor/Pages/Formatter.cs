namespace MeercatMonitor.Pages
{
    public static class Formatter
    {
        public static string FormatText(OnlineStatusStore.Status status) => status switch
        {
            OnlineStatusStore.Status.Unknown => "(?)",
            OnlineStatusStore.Status.Online => "✅ online",
            OnlineStatusStore.Status.Offline => "🛑 offline",
            _ => throw new NotImplementedException(),
        };

        public static string FormatIcon(OnlineStatusStore.Status status) => status switch
        {
            OnlineStatusStore.Status.Unknown => "(?)",
            OnlineStatusStore.Status.Online => "✅",
            OnlineStatusStore.Status.Offline => "🛑",
            _ => throw new NotImplementedException(),
        };
    }
}
