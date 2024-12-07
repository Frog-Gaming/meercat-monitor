namespace MeercatMonitor;

public record Config(int CheckIntervalS, MonitorGroup[] Monitors, MailAddress Sender, MailServer MailServer);

public record MonitorGroup(string[] Addresses, MailAddress[] Recipients, Texts Texts);

public record MailAddress(string Name, string Address);
public record MailServer(string Address, int Port, bool IgnoreCertValidation = true);

public record Texts(string SubjWentOnline, string SubjWentOffline, string BodyWentOnline, string BodyWentOffline);
