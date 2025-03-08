using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeercatMonitor.Pages
{
    internal class IndexPageModel(Config _config, OnlineStatusStore _store) : PageModel
    {
        public StatusPageModels.GroupResultsModel[] GroupResults { get; private set; } = [];

        public void OnGet()
        {
            GroupResults = [.. _config.Monitors.Select(x => StatusPageModels.GetGroupResults(x, _store))];
        }
    }
}
