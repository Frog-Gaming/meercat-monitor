using MeercatMonitor.Pages.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeercatMonitor.Pages
{
    internal class IndexPageModel(Config _config, OnlineStatusStore _store) : PageModel
    {
        public GroupResultsModel[] GroupResults { get; private set; } = [];

        public void OnGet()
        {
            GroupResults = [.. _config.Monitors.Select(x => ModelTransformer.GetGroupResults(x, _store))];
        }
    }
}
