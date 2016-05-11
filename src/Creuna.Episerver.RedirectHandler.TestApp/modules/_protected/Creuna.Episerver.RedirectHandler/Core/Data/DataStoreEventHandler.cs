using System;
using System.Reflection;
using System.Threading;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using EPiServer.Events;
using EPiServer.Events.Clients;
using EPiServer.Logging.Compatibility;
using EPiServer.PlugIn;
using EPiServer.ServiceLocation;

namespace Creuna.Episerver.RedirectHandler.Core.Data
{
    [ServiceConfiguration(typeof(IDataStoreEventHandler), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DataStoreEventHandlerHook : PlugInAttribute, IDataStoreEventHandler
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Guid _dataStoreUpdateEventId = new Guid("{26A1CA35-1CBD-44a7-8243-5E80D79F3F26}");
        private static readonly Guid _dataStoreUpdateRaiserId = new Guid("{6180555A-7A0E-4485-B1B1-44BF6E4D4A0D}");

        public static void Start()
        {
            try
            {
                if (Event.EventsEnabled)
                {
                    _log.DebugFormat("Begin: Initializing Data Store Invalidation Handler on '{0}'",
                        Environment.MachineName);

                    _log.DebugFormat("Domain ID: '{0}', Friendly Name: '{1}', Basedir: '{2}', Thread: '{3}'",
                        AppDomain.CurrentDomain.Id,
                        AppDomain.CurrentDomain.FriendlyName,
                        AppDomain.CurrentDomain.BaseDirectory,
                        Thread.CurrentThread.ManagedThreadId);
                    // Listen to events
                    Event dataStoreInvalidationEvent = Event.Get(_dataStoreUpdateEventId);
                    dataStoreInvalidationEvent.Raised += dataStoreInvalidationEvent_Raised;

                    _log.DebugFormat("End: Initializing Data Store Invalidation Handler on '{0}'",
                        Environment.MachineName);
                }
                else
                    _log.DebugFormat(
                        "NOT Initializing Data Store Invalidation Handler on '{0}'. Events are disabled for this site.",
                        Environment.MachineName);
            }
            catch (Exception ex)
            {
                _log.Error("Cannot Initialize Data Store Invalidation Handler Correctly", ex);
            }
        }

        private static void dataStoreInvalidationEvent_Raised(object sender, EventNotificationEventArgs e)
        {
            _log.DebugFormat("dataStoreInvalidationEvent '{2}' handled - raised by '{0}' on '{1}'", e.RaiserId,
                Environment.MachineName, e.EventId);
            _log.DebugFormat("Begin: Clearing cache on '{0}'", Environment.MachineName);
            ClearCache();
            _log.DebugFormat("End: Clearing cache on '{0}'", Environment.MachineName);
        }

        public static void DataStoreUpdated()
        {
            ServiceLocator.Current.GetInstance<IDataStoreEventHandler>().DataStoreUpdated();
        }

        void IDataStoreEventHandler.DataStoreUpdated()
        {
            // File is changing, notify the other servers
            Event dataStoreInvalidateEvent = Event.Get(_dataStoreUpdateEventId);
            // Raise event
            dataStoreInvalidateEvent.Raise(_dataStoreUpdateRaiserId, null);
        }

        private static void ClearCache()
        {
            ServiceLocator.Current.GetInstance<CustomRedirectHandler>().ClearCache();
        }
    }

    public interface IDataStoreEventHandler
    {
        void DataStoreUpdated();
    }
}