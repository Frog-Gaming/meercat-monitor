using MailKit.Net.Smtp;
using MimeKit;

namespace MeercatMonitor;

internal static class EmailSender
{
    public static void Send(Config config, string websiteAddress, bool websiteIsOnline)
    {
        var message = CreateMessage(config.Sender, config.Recipient);
        SetMessageText(message, websiteAddress, websiteIsOnline);

        Send(message, config);
    }

    // Distinct() is a workaround for duplicate config list values
    private static MimeMessage CreateMessage(MailAddress sender, MailAddress[] recipients) => CreateMessage(ConvertAddress(sender), recipients.Distinct().Select(ConvertAddress));

    private static MimeMessage CreateMessage(MailboxAddress sender, IEnumerable<MailboxAddress> recipients)
    {
        var message = new MimeMessage();
        message.From.Add(sender);
        message.To.AddRange(recipients);
        return message;
    }

    private static MailboxAddress ConvertAddress(MailAddress addr) => new(addr.Name, addr.Address);

    private static void SetMessageText(MimeMessage message, string websiteAddress, bool websiteIsOnline)
    {
        message.Subject = $"GAWWK GAWWK your website is {(websiteIsOnline ? "up again ✅" : "down 🛑")}";

        message.Body = new TextPart("plain")
        {
            Text = $"""
                🐿️🥜
                Your website {websiteAddress} is {(websiteIsOnline ? "up again ✅" : "down 🛑")}. lol 👌
                """
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
        message.Subject = "Meercat Monitor Test Email";
        message.Body = new TextPart() { Text = "This is a test email from Meercat Monitor" };

        Send(message, config);
    }
}
