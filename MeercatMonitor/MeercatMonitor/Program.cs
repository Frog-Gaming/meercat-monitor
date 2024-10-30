using MeercatMonitor;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile(File.Exists("appsettings.development.json") ? "appsettings.development.json" : "appsettings.json").Build().Get<Config>() ?? throw new InvalidOperationException();

var monitor = new OnlineMonitor(config);
monitor.WebsiteWentOnline += (sender, websiteAddress) => EmailSender.Send(config, websiteAddress);
monitor.WebsiteWentOffline += (sender, websiteAddress) => EmailSender.Send(config, websiteAddress);
await monitor.StartAsync();
