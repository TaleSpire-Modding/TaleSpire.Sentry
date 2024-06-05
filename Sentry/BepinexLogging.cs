using BepInEx.Logging;
using Sentry;
using System;

namespace Logging
{
    public abstract class BepinexLogging
    {
        protected ManualLogSource Logger { get; }
 
        private SentryOptions _sentryOptions;
        private EventHandler<LogEventArgs> logEvent;

        protected SentryOptions sentryOptions
        {
            get => _sentryOptions; 
            set {
                if (logEvent != null)
                {
                    Logger.LogEvent -= logEvent;
                    logEvent = null;
                }
                if (value != null)
                {
                    logEvent = GenerateSentryLogFowarding(value, GetType().Assembly.GetName().Version.ToString());
                    Logger.LogEvent += logEvent;
                }
                _sentryOptions = value; 
            }
        }

        protected BepinexLogging()
        {
            string LoggerID = GetType().AssemblyQualifiedName;
            Logger = BepInEx.Logging.Logger.CreateLogSource(LoggerID);
        }

        /// <inheritdoc cref="GenerateSentryLogFowarding(SentryOptions, string)"/>
        private EventHandler<LogEventArgs> GenerateSentryLogFowarding(string pluginVersion)
            => GenerateSentryLogFowarding(sentryOptions, pluginVersion);

        /// <summary>
        /// Generates a log forwarding function for Sentry.
        /// </summary>
        public static EventHandler<LogEventArgs> GenerateSentryLogFowarding(SentryOptions sentryOptions, string pluginVersion)
        {
            void output(object sender, LogEventArgs e)
            {
                void _scope(Scope scope)
                {
                    scope.User = new User
                    {
                        Username = BackendManager.TSUserID.Value.ToString(),
                    };
                    scope.Release = pluginVersion;
                }

                using (SentrySdk.Init(sentryOptions))
                {
                    switch (e.Level)
                    {
                        case LogLevel.Fatal:
                            SentrySdk.CaptureMessage(e.Data.ToString(), _scope, SentryLevel.Fatal);
                            break;
                        case LogLevel.Error:
                            SentrySdk.CaptureMessage(e.Data.ToString(), _scope, SentryLevel.Error);
                            break;
                        case LogLevel.Warning:
                            SentrySdk.CaptureMessage(e.Data.ToString(), _scope, SentryLevel.Warning);
                            break;
                    }
                }
            }

            return output;
        }
    }
}