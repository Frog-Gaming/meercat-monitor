namespace MeercatMonitor;

public record Result(Status Status, DateTimeOffset Time, TimeSpan ResponseTime, string ResponseDetails);
