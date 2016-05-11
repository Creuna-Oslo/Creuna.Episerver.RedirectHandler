using System.Web;
using Creuna.Episerver.RedirectHandler.Core.Configuration;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using Creuna.Episerver.RedirectHandler.Core.Data;
using Creuna.Episerver.RedirectHandler.Core.Upgrade;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;

namespace Creuna.Episerver.RedirectHandler.Core.Initialization
{
    /// <summary>
    ///     Global File Not Found Handler, for handling Asp.net exceptions
    /// </summary>
    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class Custom404HandlerInitialization : IInitializableHttpModule
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Custom404HandlerInitialization));
        public Injected<Custom404Handler> RedirectHandler { get; set; }

        public void Initialize(InitializationEngine context)
        {
            Log.Debug("Initializing 404 handler version check");
            var dba = DataAccessBaseEx.GetWorker();
            var version = dba.CheckModuleVersion();
            if (version != RedirectConfiguration.CurrentVersion)
                StartUpgrade(version);
            else
                Upgrader.Valid = true;
        }

        private static void StartUpgrade(int version)
        {
            Log.Debug("Older version found. Version nr. :" + version);
            Upgrader.Start(version);
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void InitializeHttpEvents(HttpApplication application)
        {
            application.EndRequest += RedirectHandler.Service.FileNotFoundHandler;
        }

        public void Preload(string[] parameters)
        {
        }
    }
}