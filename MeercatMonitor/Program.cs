using MeercatMonitor;
using Microsoft.Extensions.Configuration;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
    .CreateLogger();

var config = LoadConfigFile();

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

static Config LoadConfigFile()
{
    var configFile = File.Exists("appsettings.development.json") ? "appsettings.development.json" : "appsettings.json";

    return new ConfigurationBuilder()
        .AddJsonFile(configFile)
        .Build()
        // We are missing out on configuration validation here. We should make use of ErrorOnUnknownConfiguration but also ensure that no expected required configuration is missing.
        .Get<Config>() ?? throw new InvalidOperationException();
}
