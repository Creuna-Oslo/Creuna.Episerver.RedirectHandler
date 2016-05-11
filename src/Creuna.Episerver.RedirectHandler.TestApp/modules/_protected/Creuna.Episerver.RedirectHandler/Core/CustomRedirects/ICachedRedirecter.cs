namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    public interface ICachedRedirecter : IRedirecter
    {
        void ClearCache();
        long GetCachedRedirectsCount();
    }
}
