using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static MeercatMonitor.Pages.IndexPageModel;

namespace MeercatMonitor.Pages
{
    internal class GroupPageModel(Config _config, OnlineStatusStore _store) : PageModel
    {
        public GroupResultsModel? GroupResults { get; private set; }

        public void OnGet([FromRoute] string slug)
        {
            var group = _config.Monitors.SingleOrDefault(x => x.Slug == slug);
            if (group is null) return;

            GroupResults = GetGroupResults(group);

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
