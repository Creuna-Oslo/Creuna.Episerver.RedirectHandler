using EPiServer.ServiceLocation;
using System;
using System.Configuration;
using System.Globalization;

namespace Creuna.Episerver.RedirectHandler.Core.Configuration
{
    public enum FileNotFoundMode
    {
        /// <summary>
        /// </summary>
        On,

        /// <summary>
        /// </summary>
        Off,

        /// <summary>
        /// </summary>
        RemoteOnly
    };

    public enum LoggerMode
    {
        On,
        Off
    };


    /// <summary>
    ///     Configuration utility class for the custom 404 handler
    /// </summary>
    [ServiceConfiguration(typeof(RedirectConfiguration), Lifecycle = ServiceInstanceScope.Singleton)]
    public class RedirectConfiguration
    {
        private const string DEF_REDIRECTS_XML_FILE = "~/CustomRedirects.config";
        private const string DEF_NOTFOUND_PAGE = "~/notfound.aspx";
        private const LoggerMode DEF_LOGGING = LoggerMode.On;
        private const int DEF_BUFFER_SIZE = 30;
        private const int DEF_THRESHHOLD = 5;
        private const int DEF_REQUESTS_LOGGING_ACCUMULATION_TIME_SECONDS = 10;
        private const string KEY_ERROR_FALLBACK = "EPfBVN404UseStdErrorHandlerAsFallback";
        private const FileNotFoundMode DEF_NOTFOUND_MODE = FileNotFoundMode.On;
        public const int CurrentVersion = 3;
        // private static LoggerMode _logging = DEF_LOGGING;
        private static FileNotFoundMode? _handlerMode = DEF_NOTFOUND_MODE;
        //private static bool _handlerMode_IsRead;
        private bool? _fallbackToEPiServerError;
        private static int _loggingBufferSize = -1;
        private static int _loggingThresholdSize = -1;
        private static int _redirectsLoggingAccumulationTimeSeconds = -1;

        /// <summary>
        ///     Tells the errorhandler to use EPiServer Exception Manager
        ///     to render unhandled errors. Defaults to False.
        /// </summary>
        public virtual bool FallbackToEPiServerErrorExceptionManager
        {
            get
            {
                if (!_fallbackToEPiServerError.HasValue)
                {
                    bool value = false;
                    if (ConfigurationManager.AppSettings[KEY_ERROR_FALLBACK] != null)
                        bool.TryParse(ConfigurationManager.AppSettings[KEY_ERROR_FALLBACK],
                            out value);
                    _fallbackToEPiServerError = value;
                }
                return _fallbackToEPiServerError.Value;
            }
        }

        /// <summary>
        ///     The mode to use for the 404 handler
        /// </summary>
        public virtual FileNotFoundMode FileNotFoundHandlerMode
        {
            get
            {
                if (!_handlerMode.HasValue)
                {
                    string mode = ConfigurationManager.AppSettings["FileNotFoundHandlerMode"];
                    if (mode == null)
                        mode = DEF_NOTFOUND_MODE.ToString();

                    try
                    {
                        _handlerMode =
                            (FileNotFoundMode)Enum.Parse(typeof(FileNotFoundMode), mode, true /* Ignores case */);
                    }
                    catch
                    {
                        _handlerMode = DEF_NOTFOUND_MODE;
                    }
                }
                return _handlerMode.Value;
            }
        }

        /// <summary>
        ///     The mode to use for the 404 handler
        /// </summary>
        public virtual LoggerMode Logging
        {
            get { return bool.Parse(ConfigurationManager.AppSettings["RedirectLogging"] ?? "True") ? LoggerMode.On : LoggerMode.Off; }
        }


        /// <summary>
        ///     The virtual path to the 404 handler aspx file.
        /// </summary>
        public virtual string FileNotFoundHandlerPage
        {
            get { return ConfigurationManager.AppSettings["FileNotFoundHandlerPage"] ?? DEF_NOTFOUND_PAGE; }
        }

        /// <summary>
        ///     The relative path to the custom redirects xml file, including the name of the
        ///     xml file. The 404 handler will map the result to a server path.
        /// </summary>
        public virtual string CustomRedirectsXmlFile
        {
            get
            {
                return ConfigurationManager.AppSettings["CustomRedirectsXmlFile"] ?? DEF_REDIRECTS_XML_FILE;
            }
        }


        /// <summary>
        ///     BufferSize for logging of redirects.
        /// </summary>
        public virtual int BufferSize
        {
            get
            {
                if (_loggingBufferSize == -1)
                {
                    var configured = ConfigurationManager.AppSettings["RedirectsLoggingBufferSize"] ??
                                     DEF_BUFFER_SIZE.ToString(CultureInfo.InvariantCulture);
                    int parsed;
                    if (int.TryParse(configured, out parsed))
                        _loggingBufferSize = parsed;
                    else
                        _loggingBufferSize = DEF_BUFFER_SIZE;
                }
                return _loggingBufferSize;
            }
        }

        /// <summary>
        ///     ThreshHold value for redirect logging.
        /// </summary>
        public virtual int ThreshHold
        {
            get
            {
                if (_loggingThresholdSize == -1)
                {
                    var configured = ConfigurationManager.AppSettings["RedirectsLoggingThresholdSize"] ??
                                     DEF_THRESHHOLD.ToString(CultureInfo.InvariantCulture);
                    int parsed;
                    if (int.TryParse(configured, out parsed))
                        _loggingThresholdSize = parsed;
                    else
                        _loggingThresholdSize = DEF_THRESHHOLD;
                }

                return _loggingThresholdSize;
            }
        }

        /// <summary>
        /// Redirects logging accumulation time in seconds.
        /// </summary>
        public virtual int RedirectsLoggingAccumulationTimeSeconds
        {
            get
            {
                if (_redirectsLoggingAccumulationTimeSeconds == -1)
                {
                    var configuredString = ConfigurationManager.AppSettings["RedirectsLoggingAccumulationTimeSeconds"];
                    int configuredInt;

                    if (!string.IsNullOrEmpty(configuredString) && int.TryParse(configuredString, out configuredInt) && configuredInt > 0)
                    {
                        _redirectsLoggingAccumulationTimeSeconds = configuredInt;
                    }
                    else
                    {
                        _redirectsLoggingAccumulationTimeSeconds = DEF_REQUESTS_LOGGING_ACCUMULATION_TIME_SECONDS;
                    }
                }


                return _redirectsLoggingAccumulationTimeSeconds;
            }
        }
    }
}