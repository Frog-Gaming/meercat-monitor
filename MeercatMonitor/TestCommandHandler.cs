namespace MeercatMonitor;

public static class TestCommandHandler
{
    public enum Result
    {
        /// <summary>No test command identified</summary>
        None,
        /// <summary>Test command identified and handled - the operation compelted, program is expected to end</summary>
        Complete,
        /// <summary>Test command for test setup identified and handled - program is expected to continue</summary>
        SetUp,
    }

    public static Result HandleTestCommands(Serilog.Extensions.Hosting.ReloadableLogger log, Config config)
    {
        foreach (var arg in Environment.GetCommandLineArgs().Skip(count: 1))
        {
            var testemail = "--testemail=";
            if (arg.StartsWith(testemail))
            {
                var recipient = arg[testemail.Length..];
                log.Information("Sending test email to {Recipient}…", recipient);
                EmailSender.SendTestEmail(config, recipient);
                log.Information("Sent test email to {Recipient}.", recipient);
                return Result.Complete;
            }
        }

        return Result.None;
    }
}
