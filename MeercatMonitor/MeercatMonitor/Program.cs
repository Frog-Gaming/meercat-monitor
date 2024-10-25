using MailKit.Net.Smtp;
using MeercatMonitor;
using Microsoft.Extensions.Configuration;
using MimeKit;


var config = new ConfigurationBuilder().AddJsonFile(File.Exists("appsettings.development.json") ? "appsettings.development.json" : "appsettings.json").Build().Get<Config>() ?? throw new InvalidOperationException();

var timer = new PeriodicTimer(TimeSpan.FromSeconds(config.CheckIntervalS));

while (await timer.WaitForNextTickAsync())
{
    using HttpClient c = new HttpClient();

    foreach (var websiteAddress in config.WebsiteAddress)
    {
        HttpResponseMessage res = await c.GetAsync(websiteAddress);

        if (res.IsSuccessStatusCode) continue;

        // send mail
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
