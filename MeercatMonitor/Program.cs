using MeercatMonitor;

var config = LoadConfigFile();

var testArg = "--testemail=";
if (args.Length > 0 && args[0].StartsWith(testArg))
{
    using var logger = CreateLogger();
    var recipient = args[0][testArg.Length..];
    logger.Information("Sending test email to {Recipient}…", recipient);
    EmailSender.SendTestEmail(config, recipient);
    logger.Information("Sent test email to {Recipient}.", recipient);
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<OnlineMonitor>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddSerilog(ConfigureLogger);
builder.Services.AddSingleton(config);
builder.Services.AddRazorPages();

var host = builder.Build();
host.UseStaticFiles();
host.UseRouting();
host.MapRazorPages();
await host.RunAsync();



static Config LoadConfigFile()
{
    var configFile = File.Exists("appsettings.development.json") ? "appsettings.development.json" : "appsettings.json";

    return new ConfigurationBuilder()
        .AddJsonFile(configFile)
        .Build()
        // We are missing out on configuration validation here. We should make use of ErrorOnUnknownConfiguration but also ensure that no expected required configuration is missing.
        .Get<Config>() ?? throw new InvalidOperationException();
}

static void ConfigureLogger(LoggerConfiguration x) => x
    .MinimumLevel.Debug()
    .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}");

static Serilog.Core.Logger CreateLogger()
{
    var loggerConfiguration = new LoggerConfiguration();
    ConfigureLogger(loggerConfiguration);
    return loggerConfiguration.CreateLogger();
}
