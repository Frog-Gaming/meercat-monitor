using MeercatMonitor;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile(File.Exists("appsettings.development.json") ? "appsettings.development.json" : "appsettings.json").Build().Get<Config>() ?? throw new InvalidOperationException();

var testArg = "--testemail=";
if (args.Length > 0 && args[0].StartsWith(testArg))
{
    var recipient = args[0][testArg.Length..];
    Console.WriteLine($"Sending test email to {recipient}…");
    EmailSender.SendTestEmail(config, recipient);
    Console.WriteLine($"Sent test email to {recipient}.");
    return;
}

var monitor = new OnlineMonitor(config);
monitor.WebsiteWentOnline += (sender, ev) => EmailSender.Send(config, ev.address, ev.isOnline);
monitor.WebsiteWentOffline += (sender, ev) => EmailSender.Send(config, ev.address, ev.isOnline);
await monitor.StartAsync();
