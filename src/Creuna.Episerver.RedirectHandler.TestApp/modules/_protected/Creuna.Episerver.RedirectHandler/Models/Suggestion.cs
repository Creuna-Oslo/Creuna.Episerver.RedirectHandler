using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;

namespace Creuna.Episerver.RedirectHandler.Models
{
    public class Suggestion
    {
        public Suggestion(CustomRedirect customRedirect, int pageNumber, int pageSize, string searchWord)
        {
            CustomRedirect = customRedirect;
            PageNumber = pageNumber;
            PageSize = pageSize;
            SearchWord = searchWord;
        }

        public CustomRedirect CustomRedirect { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string SearchWord { get; set; }
    }
}