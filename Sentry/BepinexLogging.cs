using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using PluginUtilities;
using Sentry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace Logging
{
    [BepInPlugin(Guid, "Config Manager", Version)]
    [BepInDependency(SetInjectionFlag.Guid)]
    public sealed class BepinexLogging : BaseUnityPlugin
    {
        /// <summary>
        /// Plugin Attributes
        /// </summary>
        public const string Guid = "com.hf.hollofox.sentry";
        public const string Name = "Bepinex To Sentry Logging";
        public const string Version = "1.0.1.0";
        private const BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        /// <summary>
        /// 
        /// </summary>
        private bool addLogForwarding = true;
        private readonly Dictionary<string, string> ApiKeyLookup = new Dictionary<string, string>() {
            
        };

        public BepinexLogging()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (addLogForwarding)
            {
                addLogForwarding = false;

                Logger.LogInfo("Initializing Sentry Log Forwarding");

                Chainloader.Plugins
                .Concat(FindObjectsOfType(typeof(BaseUnityPlugin)).Cast<BaseUnityPlugin>()).Distinct()
                .Select(p => (plugin: p,metadata: MetadataHelper.GetMetadata(p))).ToList()
                .ForEach(p =>
                {
                    var plugin = p.plugin;
                    var metadata = p.metadata;
                    if (ApiKeyLookup.TryGetValue(p.metadata.Name, out string apiKey))
                    {
                        Logger.LogInfo($"Fetching logSource for {metadata.Name}");
                        ManualLogSource manualLogSource = (ManualLogSource)typeof(BaseUnityPlugin).GetProperty("Logger", bindFlags).GetValue(plugin); // use sys reflect to fetch

                        if (manualLogSource != null)
                        {
                            Logger.LogInfo($"Found {metadata.Name} manual log source");
                            SentryOptions sentryOptions = new SentryOptions
                            {
                                Dsn = apiKey,
                                Debug = true,
                                TracesSampleRate = 1,
                                IsGlobalModeEnabled = false,
                                AttachStacktrace = true
                            };
                            Logger.LogInfo($"{metadata.Name} sentry options created");

                            manualLogSource.LogEvent += GenerateSentryLogFowarding(sentryOptions, metadata.Version.ToString());
                            Logger.LogInfo($"Added Sentry Log Forwarding for {metadata.Name}");
                        }
                    }
                });

                Logger.LogInfo("Sentry Log Forwarding Initialized");
            }
        }

        private static readonly HashSet<LogLevel> accepted = new HashSet<LogLevel> { LogLevel.Fatal, LogLevel.Error, LogLevel.Warning };

        /// <summary>
        /// Generates a log forwarding function for Sentry.
        /// </summary>
        public static EventHandler<LogEventArgs> GenerateSentryLogFowarding(SentryOptions sentryOptions, string pluginVersion)
        {
            void output(object sender, LogEventArgs e)
            {
                if (accepted.Contains(e.Level))
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
            }

            return output;
        }
    }
}