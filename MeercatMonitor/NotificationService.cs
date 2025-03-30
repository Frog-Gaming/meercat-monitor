namespace MeercatMonitor;

public class NotificationService(EmailSender _email)
{
    public void HandleStatusChange(MonitorTarget target, bool isOnline)
    {
        _email.SendFor(target, isOnline);
    }
}
