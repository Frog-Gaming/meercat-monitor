namespace MeercatMonitor;

public record Config(int CheckIntervalS, MonitorGroup[] Monitors, MailAddress Sender, MailServer MailServer);

public record MonitorGroup(string[] Addresses, MailAddress[] Recipients);

public record MailAddress(string Name, string Address);
public record MailServer(string Address, int Port, bool IgnoreCertValidation = true);
