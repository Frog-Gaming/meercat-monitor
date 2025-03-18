using MeercatMonitor;

using var log = CreateBootstrapLogger();

var config = LoadConfigFile();

var testResult = TestCommandHandler.HandleTestCommands(log, config);
if (testResult is TestCommandHandler.Result.Complete) return 0;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<OnlineMonitor>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddSingleton<OnlineStatusStore>();
builder.Services.AddSerilog(ConfigureLogger);
builder.Services.AddSingleton(config);
builder.Services.AddRazorPages();

if (args.Contains("--test"))
{
    builder.Services.AddSingleton(new TestConfig(SendMonitorRequests: false, SendEmails: true, FillTestData: 100));
}

var host = builder.Build();
host.UseStaticFiles();
host.UseRouting();
host.MapRazorPages();
//host.UseDeveloperExceptionPage();
await host.RunAsync();

return 0;

static Config LoadConfigFile()
{
    var configFile = File.Exists("appsettings.development.json") ? "appsettings.development.json" : "appsettings.json";

    return new ConfigurationBuilder()
        .AddJsonFile(configFile)
        .Build()
        // We are missing out on configuration validation here. We should make use of ErrorOnUnknownConfiguration but also ensure that no expected required configuration is missing.
        .Get<Config>() ?? throw new InvalidOperationException();
}

static Serilog.Extensions.Hosting.ReloadableLogger CreateBootstrapLogger()
{
    var logConf = new LoggerConfiguration();
    ConfigureLogger(logConf);
    return logConf.CreateBootstrapLogger();
}

static void ConfigureLogger(LoggerConfiguration x) => x
    .MinimumLevel.Debug()
    .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}");
