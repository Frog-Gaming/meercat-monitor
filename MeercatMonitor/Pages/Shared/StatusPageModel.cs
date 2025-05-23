namespace MeercatMonitor.Pages.Shared;

public static class ModelTransformer
{
    public static GroupResultsModel GetGroupResults(MonitorGroup group, OnlineStatusStore _store)
    {
        TargetResultsModel[] results = [.. group.Targets.Select(x => GetTargetStatus(x, _store))];
        return new(group, results);
    }

    public static TargetResultsModel GetTargetStatus(MonitorTarget target, OnlineStatusStore _store)
    {
        return new TargetResultsModel(target, [.. _store.GetValues(target)]);
    }
}
