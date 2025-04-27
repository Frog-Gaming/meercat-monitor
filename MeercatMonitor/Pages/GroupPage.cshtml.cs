using MeercatMonitor.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeercatMonitor.Pages
{
    internal class GroupPageModel(Config _config, OnlineStatusStore _store) : PageModel
    {
        public GroupResultsModel? GroupResults { get; private set; }

        public void OnGet([FromRoute] string slug)
        {
            var group = _config.Monitors.SingleOrDefault(x => x.Slug == slug);
            if (group is null) return;

            GroupResults = ModelTransformer.GetGroupResults(group, _store);
        }
    }
}
