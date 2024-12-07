using MeercatMonitor;
using Microsoft.Extensions.Configuration;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var config = new ConfigurationBuilder().AddJsonFile(File.Exists("appsettings.development.json") ? "appsettings.development.json" : "appsettings.json").Build().Get<Config>() ?? throw new InvalidOperationException();

var testArg = "--testemail=";
if (args.Length > 0 && args[0].StartsWith(testArg))
{
    var recipient = args[0][testArg.Length..];
    Log.Information("Sending test email to {Recipient}…", recipient);
    EmailSender.SendTestEmail(config, recipient);
    Log.Information("Sent test email to {Recipient}.", recipient);
    return;
}

var monitor = new OnlineMonitor(config);
monitor.WebsiteWentOnline += (sender, ev) => EmailSender.Send(config, ev.address, ev.isOnline);
monitor.WebsiteWentOffline += (sender, ev) => EmailSender.Send(config, ev.address, ev.isOnline);
await monitor.StartAsync();

await Log.CloseAndFlushAsync();
