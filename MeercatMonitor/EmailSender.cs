using MailKit.Net.Smtp;
using MimeKit;

namespace MeercatMonitor;

internal static class EmailSender
{
    public static void Send(Config config, string websiteAddress, bool websiteIsOnline)
    {
        foreach (var monitor in config.Monitors.Where(x => x.Addresses.Contains(websiteAddress)))
        {
            // Distinct() as a workaround for duplicate config list values
            var recipients = monitor.Recipients.Distinct().ToArray();
            var message = CreateMessage(config.Sender, recipients);
            SetMessageText(message, websiteAddress, websiteIsOnline, monitor.Texts);

            Send(message, config);
        }
    }

    private static MimeMessage CreateMessage(MailAddress sender, MailAddress[] recipients) => CreateMessage(ConvertAddress(sender), recipients.Select(ConvertAddress));

    private static MimeMessage CreateMessage(MailboxAddress sender, IEnumerable<MailboxAddress> recipients)
    {
        var message = new MimeMessage();
        message.From.Add(sender);
        message.To.AddRange(recipients);
        return message;
    }

    private static MailboxAddress ConvertAddress(MailAddress addr) => new(addr.Name, addr.Address);

    private static void SetMessageText(MimeMessage message, string websiteAddress, bool websiteIsOnline, Texts texts)
    {
        message.Subject = websiteIsOnline ? texts.SubjWentOnline : texts.SubjWentOffline;

        string bodyText = (websiteIsOnline ? texts.BodyWentOnline : texts.BodyWentOffline).Replace("{websiteAddress}", websiteAddress);
        message.Body = new TextPart("plain")
        {
            Text = bodyText,
        };
    }

    private static void Send(MimeMessage message, Config config)
    {
        using var client = new SmtpClient();

        // ignore certificate validation issues
        if (config.MailServer.IgnoreCertValidation) client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

        try
        {
            client.Connect(config.MailServer.Address, config.MailServer.Port, useSsl: false);
            client.Send(message);
            client.Disconnect(true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Exception during email send: {ex.Message}");
        }
    }

    public static void SendTestEmail(Config config, string recipientAddress)
    {
        var message = CreateMessage(ConvertAddress(config.Sender), [new MailboxAddress(recipientAddress, recipientAddress)]);
        Texts texts = new Texts(
            SubjWentOnline: "GAWWK GAWWK your website is up again",
            SubjWentOffline: "GAWWK GAWWK your website is down",
            BodyWentOnline: "🐿️🥜 Your website {websiteAddress} is up again. lol 👌",
            BodyWentOffline: "🐿️🥜 Your website {websiteAddress} is down. lol 👌"
        );
        SetMessageText(message, "<fake-website-for-testing>", websiteIsOnline: true, texts);

        Send(message, config);
    }
}
