namespace MeercatMonitor;

public record Config(int CheckIntervalS, int? CheckTimeoutS, int? HistoryDisplayLimit, Retry Retry, MonitorGroup[] Monitors, MailAddress Sender, MailServer MailServer);

public record MonitorGroup(string Name, string Slug, MonitorTarget[] Targets, MailAddress[] Recipients, Texts Texts);

public record MonitorTarget(string Name, string Slug, Uri Address);

public record MailAddress(string Name, string Address);
public record MailServer(string Address, int Port, bool IgnoreCertValidation = true);

public record Texts(string SubjWentOnline, string SubjWentOffline, string? BodyPlainWentOnline = null, string? BodyPlainWentOffline = null, string? BodyHtmlWentOnline = null, string? BodyHtmlWentOffline = null);

public record TestConfig(bool SendMonitorRequests, bool SendEmails, int? FillTestData);

public record Retry(bool SecondChance);
