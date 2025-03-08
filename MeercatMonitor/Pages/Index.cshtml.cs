using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeercatMonitor.Pages
{
    internal class IndexPageModel(Config _config, OnlineStatusStore _store) : PageModel
    {
        public record GroupResultsModel(MonitorGroup Group, OnlineStatusStore.Result?[] Results);

        public GroupResultsModel[] GroupResults { get; private set; } = [];

        public void OnGet()
        {
            GroupResults = _config.Monitors.Select(GetGroupResults).ToArray();

            GroupResultsModel GetGroupResults(MonitorGroup group)
            {
                OnlineStatusStore.Result?[] results = group.Addresses.Select(GetTargetStatus).ToArray();
                return new(group, results);
            }

            OnlineStatusStore.Result? GetTargetStatus(ToMonitorAddress target)
            {
                return _store.TryGetValue(target, out var result) ? result : null;
            }
        }
    }
}
