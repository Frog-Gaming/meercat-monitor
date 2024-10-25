namespace MeercatMonitor;

public record MailAddress(string Name, string Address);
public record MailServer(string Address, int Port, bool IgnoreCertValidation = true);
public record Config(int CheckIntervalS, string[] WebsiteAddress, MailAddress Sender, MailAddress[] Recipient, MailServer MailServer);
