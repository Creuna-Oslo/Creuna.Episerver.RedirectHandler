using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Creuna.Episerver.RedirectHandler.Core.Configuration;
using Creuna.Episerver.RedirectHandler.Core.Data;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

namespace Creuna.Episerver.RedirectHandler.Core.Logging
{
    public class RequestLogger
    {
        private readonly RedirectConfiguration _redirectConfiguration;
        private static readonly RequestLogger instance = ServiceLocator.Current.GetInstance<RequestLogger>();
        private static RequestLogger instanceOverride = null;
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly Timer _logsAccumulationTimer; 

        public RequestLogger(RedirectConfiguration redirectConfiguration)
        {
            _redirectConfiguration = redirectConfiguration;
            _logsAccumulationTimer = new Timer(_redirectConfiguration.RedirectsLoggingAccumulationTimeSeconds * 1000) { AutoReset = false };
            _logsAccumulationTimer.Elapsed += LogsAccumulationTimerOnElapsed;
            LogQueue = new List<LogEvent>();
        }

        private void LogsAccumulationTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            LogRequestsQueue();
        }

        public static RequestLogger Instance
        {
            get { return instanceOverride ?? InternalInstance; }
            set { instanceOverride = value; }
        }

        internal static RequestLogger InternalInstance
        {
            get { return instance; }
        }

        private readonly object _logQueueSyncRoot = new object();

        private List<LogEvent> LogQueue { get; set; }

        public virtual void LogRequest(string oldUrl, string referrer)
        {
            int bufferSize = _redirectConfiguration.BufferSize;
            if (LogQueue.Count >= bufferSize)
            {
                lock (_logQueueSyncRoot)
                {
                    if (LogQueue.Count >= bufferSize)
                    {
                        _logsAccumulationTimer.Enabled = false;
                        LogRequestsQueue();
                    }
                }
            }

            LogQueue.Add(new LogEvent(oldUrl, DateTime.Now, referrer));
            _logsAccumulationTimer.Enabled = true;
        }

        private void LogRequestsQueue()
        {
            lock (_logQueueSyncRoot)
            {
                try
                {
                    LogRequests(LogQueue);
                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred while trying to log 404 errors. ", ex);
                }
                finally
                {
                    LogQueue = new List<LogEvent>();
                }
            }
        }

        private void LogRequests(List<LogEvent> logEvents)
        {
            Logger.Debug("Logging 404 errors to database");
            int threshold = _redirectConfiguration.ThreshHold;
            DateTime start = logEvents.First().Requested;
            DateTime end = logEvents.Last().Requested;
            double requestsTimeRangeMilliseconds = (end - start).TotalMilliseconds;
            // ReSharper disable CompareOfFloatsByEqualityOperator
            var requestsPerSecond = requestsTimeRangeMilliseconds != 0
                // ReSharper restore CompareOfFloatsByEqualityOperator
                ? logEvents.Count/requestsTimeRangeMilliseconds * 1000
                : logEvents.Count;

            if (requestsPerSecond <= threshold)
            {
                DataAccessBaseEx dba = DataAccessBaseEx.GetWorker();
                foreach (LogEvent logEvent in logEvents)
                {
                    dba.LogRequestToDb(logEvent.OldUrl, logEvent.Referer, logEvent.Requested);
                }
                Logger.Debug(string.Format("{0} 404 request(s) has been stored to the database.", logEvents.Count));
            }
            else
                Logger.Warning(
                    "404 requests have been made too frequents (exceeded the threshold). Requests not logged to database.");
        }
    }
}