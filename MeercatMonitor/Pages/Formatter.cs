namespace MeercatMonitor.Pages
{
    public static class Formatter
    {
        public static string FormatText(Status status) => status switch
        {
            Status.Unknown => "(?)",
            Status.Online => "✅ online",
            Status.Offline => "🛑 offline",
            _ => throw new NotImplementedException(),
        };

        public static string FormatIcon(Status status) => status switch
        {
            Status.Unknown => "(?)",
            Status.Online => "✅",
            Status.Offline => "🛑",
            _ => throw new NotImplementedException(),
        };

        public static string GetOnlineClass(Status status) => status switch
        {
            Status.Unknown => "unknown",
            Status.Online => "online",
            Status.Offline => "offline",
            _ => throw new NotImplementedException(),
        };
    }
}
