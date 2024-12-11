using MailKit.Net.Smtp;
using MimeKit;

namespace MeercatMonitor;

internal class EmailSender
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1118:Utility classes should not have public constructors", Justification = "False-positive")]
    public EmailSender(OnlineMonitor onlineMonitor, Config config)
    {
        onlineMonitor.WebsiteWentOnline += (sender, ev) => Send(config, ev.address, ev.isOnline);
        onlineMonitor.WebsiteWentOffline += (sender, ev) => Send(config, ev.address, ev.isOnline);
    }

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

        string bodyPlain = (websiteIsOnline ? texts.BodyPlainWentOnline : texts.BodyPlainWentOffline).Replace("{websiteAddress}", websiteAddress);
        string bodyHtml = (websiteIsOnline ? texts.BodyHtmlWentOnline : texts.BodyHtmlWentOffline).Replace("{websiteAddress}", websiteAddress);
        var builder = new BodyBuilder();
        builder.TextBody = bodyPlain;
        builder.HtmlBody = bodyHtml;
        message.Body = builder.ToMessageBody();
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
            Log.Error(ex, "Exception during email send: {Message}", ex.Message);
        }
    }

    public static void SendTestEmail(Config config, string recipientAddress)
    {
        var message = CreateMessage(ConvertAddress(config.Sender), [new MailboxAddress(recipientAddress, recipientAddress)]);
        var texts = new Texts(
            SubjWentOnline: "GAWWK GAWWK your website is up again",
            SubjWentOffline: "GAWWK GAWWK your website is down",
            BodyPlainWentOnline: "🐿️🥜 Your website {websiteAddress} is up again. lol 👌",
            BodyPlainWentOffline: "🐿️🥜 Your website {websiteAddress} is down. lol 👌",
            BodyHtmlWentOnline: "<p>🐿️🥜 Your website {websiteAddress} is <strong>up</strong> again. lol 👌</p>",
            BodyHtmlWentOffline: "<p>🐿️🥜 Your website {websiteAddress} is <strong>down</strong>. lol 👌</p>"
        );
        SetMessageText(message, "<fake-website-for-testing>", websiteIsOnline: true, texts);

        Send(message, config);
    }
}
