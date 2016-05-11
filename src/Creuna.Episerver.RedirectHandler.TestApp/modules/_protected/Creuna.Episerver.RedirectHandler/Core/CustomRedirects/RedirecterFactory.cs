using Creuna.Episerver.RedirectHandler.Core.Configuration;
using EPiServer.ServiceLocation;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    [ServiceConfiguration(typeof(RedirecterFactory), Lifecycle = ServiceInstanceScope.Singleton)]
    public class RedirecterFactory
    {
        private static readonly RedirecterFactory DefaultInstance = ServiceLocator.Current.GetInstance<RedirecterFactory>();
        private static RedirecterFactory _instanceOverride;

        public virtual IRedirecter CreateRedirecter(CustomRedirectCollection customRedirects, RedirectConfiguration redirectConfiguration)
        {
            return new DictionaryCachedRedirecter(new Redirecter(customRedirects, redirectConfiguration));
        }

        public static RedirecterFactory Current
        {
            get { return _instanceOverride ?? DefaultInstance; }
            set { _instanceOverride = value; }
        }
    }
}