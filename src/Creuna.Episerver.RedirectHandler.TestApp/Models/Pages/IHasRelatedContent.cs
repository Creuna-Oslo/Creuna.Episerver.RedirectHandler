using EPiServer.Core;

namespace Creuna.Episerver.RedirectHandler.TestApp.Models.Pages
{
    public interface IHasRelatedContent
    {
        ContentArea RelatedContentArea { get; }
    }
}
