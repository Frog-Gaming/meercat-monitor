namespace MeercatMonitor
{
    internal class NotificationService(EmailSender _email)
    {
        public void HandleStatusChange(string websiteAddress, bool isOnline)
        {
            _email.SendFor(websiteAddress, isOnline);
        }
    }
}
