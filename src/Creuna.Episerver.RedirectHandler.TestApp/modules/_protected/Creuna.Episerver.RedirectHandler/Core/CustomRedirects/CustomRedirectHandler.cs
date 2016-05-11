using System;
using Creuna.Episerver.RedirectHandler.Core.Configuration;
using Creuna.Episerver.RedirectHandler.Core.Data;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    /// <summary>
    ///     Handler for custom redirects. Loads and caches the list of custom redirects
    ///     to ensure performance.
    /// </summary>
    [ServiceConfiguration(typeof(CustomRedirectHandler), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CustomRedirectHandler
    {
        private readonly RedirectConfiguration _redirectConfiguration;
        private readonly DataStoreHandler _dataStoreHandler;
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CustomRedirectHandler));
        private IRedirecter _redirecter;

        public CustomRedirectHandler(RedirectConfiguration redirectConfiguration, DataStoreHandler dataStoreHandler)
        {
            _redirectConfiguration = redirectConfiguration;
            _dataStoreHandler = dataStoreHandler;
            _redirecter = CreateRedirecter();
        }

        private IRedirecter CreateRedirecter()
        {
            var result = RedirecterFactory.Current.CreateRedirecter(LoadCustomRedirects(), _redirectConfiguration);
            return result;
        }

        public static string CustomRedirectHandlerException { get; set; }

        /// <summary>
        ///     Save a collection of redirects, and call method to raise an event in order to clear cache on all servers.
        /// </summary>
        /// <param name="redirects"></param>
        public virtual void SaveCustomRedirects(CustomRedirectCollection redirects)
        {
            Logger.Log(Level.Debug, "Saving custom redirects");
            foreach (CustomRedirect redirect in redirects)
            {
                // Add redirect 
                _dataStoreHandler.SaveCustomRedirect(redirect);
            }
            DataStoreEventHandlerHook.DataStoreUpdated();
        }

        /// <summary>
        ///     Read the custom redirects from the dynamic data store, and
        ///     stores them in the CustomRedirect property
        /// </summary>
        protected virtual CustomRedirectCollection LoadCustomRedirects()
        {
            var customRedirects = new CustomRedirectCollection();

            foreach (var redirect in _dataStoreHandler.GetCustomRedirects(false))
                customRedirects.Add(redirect);

            return customRedirects;
        }

        /// <summary>
        ///     Clears the redirect cache.
        /// </summary>
        public virtual void ClearCache()
        {
            _redirecter = CreateRedirecter();
        }

        public virtual RedirectAttempt HandleRequest(string referer, Uri urlNotFound)
        {
            return _redirecter.Redirect(referer, urlNotFound);
        }
    }
}