using MailKit.Net.Smtp;
using MimeKit;

namespace MeercatMonitor;

internal class EmailSender
{
    public static void Send(Config config, string websiteAddress)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.Sender.Name, config.Sender.Address));

        message.To.AddRange(config.Recipient.Distinct().Select(recipient => new MailboxAddress(recipient.Name, recipient.Address)));
        message.Subject = "GAWWK GAWWK trouble on your website";

        message.Body = new TextPart("plain")
        {
            Text = $"""
                Your website {websiteAddress} is down. lol
                """
        };

        using (var client = new SmtpClient())
        {
            // ignore certificate validation issues
            if (config.MailServer.IgnoreCertValidation) client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            client.Connect(config.MailServer.Address, config.MailServer.Port, useSsl: false);

            client.Send(message);
            client.Disconnect(true);
        }
    }

}
