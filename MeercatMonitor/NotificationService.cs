namespace MeercatMonitor
{
    internal class NotificationService(EmailSender _email)
    {
        public void HandleStatusChange(ToMonitorAddress toMonitorAddress, bool isOnline)
        {
            _email.SendFor(toMonitorAddress, isOnline);
        }
    }
}
