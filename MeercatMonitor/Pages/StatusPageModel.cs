namespace MeercatMonitor.Pages
{
    public class StatusPageModels
    {
        public record GroupResultsModel(MonitorGroup Group, TargetResultsModel[] Results);
        public record TargetResultsModel(ToMonitorAddress Target, Result[] Results);

        public static GroupResultsModel GetGroupResults(MonitorGroup group, OnlineStatusStore _store)
        {
            TargetResultsModel[] results = [.. group.Addresses.Select(x => GetTargetStatus(x, _store))];
            return new(group, results);
        }

        public static TargetResultsModel GetTargetStatus(ToMonitorAddress target, OnlineStatusStore _store)
        {
            return new TargetResultsModel(target, [.. _store.GetValues(target)]);
        }
    }
}
