using System.Net;
using MailKit.Net.Smtp;
using MimeKit;

namespace MeercatMonitor;

internal class EmailSender(Config _config, TestConfig _testConfig)
{
    public void SendFor(ToMonitorAddress toMonitorAddress, bool isOnline)
    {
        foreach (var monitorGroup in _config.Monitors.Where(x => x.Addresses.Contains(toMonitorAddress)))
        {
            // Distinct() as a workaround for duplicate config list values
            var recipients = monitorGroup.Recipients.Distinct().ToArray();
            var message = CreateMessage(_config.Sender, recipients);
            SetMessageText(message, toMonitorAddress, isOnline, monitorGroup.Texts);

            if (!(_testConfig?.SendEmails ?? true)) return;

            Send(message, _config);
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

    private static void SetMessageText(MimeMessage message, ToMonitorAddress toMonitorAddress, bool websiteIsOnline, Texts texts)
    {
        var subjectTemplate = websiteIsOnline ? texts.SubjWentOnline : texts.SubjWentOffline;
        message.Subject = FillTemplate(subjectTemplate, toMonitorAddress, html: false);

        var builder = new BodyBuilder();
        if (websiteIsOnline && texts.BodyPlainWentOnline is not null)
        {
            builder.TextBody = FillTemplate(texts.BodyPlainWentOnline, toMonitorAddress, html: false);
        }
        if (!websiteIsOnline && texts.BodyPlainWentOffline is not null)
        {
            builder.TextBody = FillTemplate(texts.BodyPlainWentOffline, toMonitorAddress, html: false);
        }
        if (websiteIsOnline && texts.BodyHtmlWentOnline is not null)
        {
            builder.HtmlBody = FillTemplate(texts.BodyHtmlWentOnline, toMonitorAddress, html: true);
        }
        if (!websiteIsOnline && texts.BodyHtmlWentOffline is not null)
        {
            builder.HtmlBody = FillTemplate(texts.BodyHtmlWentOffline, toMonitorAddress, html: true);
        }
        message.Body = builder.ToMessageBody();

        static string FillTemplate(string template, ToMonitorAddress toMonitorAddress, bool html)
        {
            var websiteName = toMonitorAddress.Name;
            var websiteAddress = html ? WebUtility.HtmlEncode(toMonitorAddress.Address) : toMonitorAddress.Address;

            return template.Replace("{websiteName}", websiteName).Replace("{websiteAddress}", websiteAddress);
        }
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
            SubjWentOnline: "GAWWK GAWWK your website {websiteName} is up again",
            SubjWentOffline: "GAWWK GAWWK your website {websiteName} is down",
            BodyPlainWentOnline: "🐿️🥜 Your website {websiteAddress} is up again. lol 👌",
            BodyPlainWentOffline: "🐿️🥜 Your website {websiteAddress} is down. lol 👌",
            BodyHtmlWentOnline: "<p>🐿️🥜 Your website {websiteAddress} is <strong>up</strong> again. lol 👌</p>",
            BodyHtmlWentOffline: "<p>🐿️🥜 Your website {websiteAddress} is <strong>down</strong>. lol 👌</p>"
        );
        SetMessageText(message, new ToMonitorAddress("<fake website name>", "<fake-slug>", "<fake-website-for-testing>"), websiteIsOnline: true, texts);

        Send(message, config);
    }
}
