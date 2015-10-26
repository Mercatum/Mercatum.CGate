using System;
using System.Collections.Generic;


namespace Mercatum.CGate
{
    public static class CGateEnvironment
    {
        /// <summary>
        /// A predefined client key for the test system.
        /// </summary>
        public const string TestClientKey = "11111111";

        private static bool _opened;

        /// <summary>
        /// Initialization file path. This file describes configuration of the library — journaling mode, etc.
        /// </summary>
        public static string IniPath { get; set; }

        /// <summary>
        /// Enable or disable MQ-subsystem initialization.
        /// </summary>
        public static bool InitializeMq { get; set; }

        /// <summary>
        /// Enable or disable the replica client initialization.
        /// </summary>
        public static bool InitializeReplClient { get; set; }

        /// <summary>
        /// Client program identifier.
        /// </summary>
        public static string ClientKey { get; set; }

        /// <summary>
        /// Desired logging mode.
        /// </summary>
        public static CGateLogMode LogMode { get; set; }

        /// <summary>
        /// Minimal logging level.
        /// </summary>
        public static CGateLogLevel MinLogLevel { get; set; }

        /// <summary>
        /// Name of a section with logging settings in the configuration file (only when LogMode == CGateLogLevel.P2).
        /// </summary>
        public static string LogSettingsSection { get; set; }

        /// <summary>
        /// Tells if the environment was sucessfully initialized.
        /// </summary>
        public static bool Opened
        {
            get { return _opened; }
        }


        static CGateEnvironment()
        {
            // Set suitable default values
            LogMode = CGateLogMode.P2;
            MinLogLevel = CGateLogLevel.Debug;
        }


        public static void Open()
        {
            // TODO: thread-safe initialization
            // TODO: check IniPath?

            if( !InitializeMq && !InitializeReplClient )
                throw new InvalidOperationException("At least one subsystem should be initialized");

            if( string.IsNullOrEmpty(ClientKey) )
                throw new InvalidOperationException("Client key should be set");

            string settings = FormatSettings();
            ru.micexrts.cgate.CGate.Open(settings);
            _opened = true;
        }


        public static void Close()
        {
            ru.micexrts.cgate.CGate.Close();
            _opened = false;
        }


        public static void LogTrace(string format, params object[] args)
        {
            // TODO: check initialization status
            ru.micexrts.cgate.CGate.LogTrace(String.Format(format, args));
        }


        public static void LogDebug(string format, params object[] args)
        {
            // TODO: check initialization status
            ru.micexrts.cgate.CGate.LogDebug(String.Format(format, args));
        }


        public static void LogInfo(string format, params object[] args)
        {
            // TODO: check initialization status
            ru.micexrts.cgate.CGate.LogInfo(String.Format(format, args));
        }


        public static void LogError(string format, params object[] args)
        {
            // TODO: check initialization status
            ru.micexrts.cgate.CGate.LogError(String.Format(format, args));
        }
        
        
        private static string FormatSettings()
        {
            const string MqSubsystem = "mq";
            const string ReplClientSubsystem = "replclient";

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            // TODO: required parameter?
            if( !string.IsNullOrEmpty(IniPath) )
                parameters["ini"] = IniPath;

            string subsystems = null;

            if( InitializeMq && InitializeReplClient )
                subsystems = MqSubsystem + "," + ReplClientSubsystem;
            else if( InitializeMq )
                subsystems = MqSubsystem;
            else if( InitializeReplClient )
                subsystems = ReplClientSubsystem;

            // TODO: what to do when there are no subsystems to initialize?
            if( !string.IsNullOrEmpty(subsystems) )
                parameters["subsystems"] = subsystems;

            if( string.IsNullOrEmpty(ClientKey) )
                throw new ArgumentException("Client key cannot be null or empty");

            parameters["key"] = ClientKey;

            parameters["log"] = FormatLogMode(LogMode, LogSettingsSection);
            parameters["minloglevel"] = FormatLogLevel(MinLogLevel);

            return CGateSettingsFormatter.FormatParameters(parameters);
        }


        private static string FormatLogMode(CGateLogMode logMode,
                                            string logSection)
        {
            switch( logMode )
            {
            case CGateLogMode.Null:
                return "null";
            case CGateLogMode.Std:
                return "std";
            case CGateLogMode.P2:
                return "p2" + (string.IsNullOrEmpty(logSection) ? "" : ":" + logSection);
            }

            throw new ArgumentOutOfRangeException("logMode", logMode, "Unkwown logging mode value");
        }


        private static string FormatLogLevel(CGateLogLevel logLevel)
        {
            switch( logLevel )
            {
            case CGateLogLevel.Trace:
                return "trace";
            case CGateLogLevel.Debug:
                return "debug";
            case CGateLogLevel.Info:
                return "info";
            case CGateLogLevel.Notice:
                return "notice";
            case CGateLogLevel.Warning:
                return "warning";
            case CGateLogLevel.Error:
                return "error";
            case CGateLogLevel.Critical:
                return "critical";
            }

            throw new ArgumentOutOfRangeException("logLevel",
                                                  logLevel,
                                                  "Unkwown logging level value");
        }
    }
}
