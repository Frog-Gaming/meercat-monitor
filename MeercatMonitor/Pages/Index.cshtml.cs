using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeercatMonitor.Pages
{
    internal class IndexPageModel(OnlineStatusStore _store) : PageModel
    {
        public IReadOnlyCollection<OnlineStatusStore.Result> AllStatus { get; private set; } = [];

        public void OnGet()
        {
            AllStatus = _store.GetAll();
        }
    }
}
