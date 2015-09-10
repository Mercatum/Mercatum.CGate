using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;


namespace Mercatum.CGate
{
    /// <summary>
    /// Formats settings into strings used as inputs in many CGate functions.
    /// </summary>
    internal static class CGateSettingsFormatter
    {
        public static string FormatNewConnectionSettings(CGateConnectionTarget connectionTarget)
        {
            var settings = new Dictionary<string, string>();

            settings["app_name"] = NullToEmpty(connectionTarget.AppName);
            settings["timeout"] = connectionTarget.OpenTimeout.ToString(CultureInfo.InvariantCulture);
            settings["local_timeout"] =
                connectionTarget.LrpcqTimeout.ToString(CultureInfo.InvariantCulture);
            settings["lrpcq_buf"] =
                connectionTarget.LrpcqBufferSize.ToString(CultureInfo.InvariantCulture);

            if( !string.IsNullOrEmpty(connectionTarget.LocalPassword) )
                settings["local_pass"] = NullToEmpty(connectionTarget.LocalPassword);
            if( !string.IsNullOrEmpty(connectionTarget.Name) )
                settings["name"] = NullToEmpty(connectionTarget.Name);

            return string.Format("{0}://{1}:{2};{3}",
                                 FormatConnectionType(connectionTarget.Type),
                                 connectionTarget.Host,
                                 connectionTarget.Port,
                                 FormatKeyValuePairs(settings));
        }


        public static string FormatNewListenerSettings(CGateListenerType listenerType,
                                                       string streamName)
        {
            return string.Format("{0}://{1};{2}",
                                 FormatListenerType(listenerType),
                                 NullToEmpty(streamName),
                                 "");
        }


        private static string FormatConnectionType(CGateConnectionType connectionType)
        {
            switch( connectionType )
            {
            case CGateConnectionType.Tcp:
                return "p2tcp";

            case CGateConnectionType.Lrpcq:
                return "p2lrpcq";

            case CGateConnectionType.Sys:
                return "p2sys";
            }

            throw new ArgumentOutOfRangeException("connectionType",
                                                  connectionType,
                                                  "Unknown connection type");
        }


        private static string FormatListenerType(CGateListenerType listenerType)
        {
            switch( listenerType )
            {
            case CGateListenerType.Replication:
                return "p2repl";

            case CGateListenerType.OrderBook:
                return "p2ordbook";

            case CGateListenerType.MqReply:
                return "p2mqreply";

            case CGateListenerType.Sys:
                return "p2sys";
            }

            throw new ArgumentOutOfRangeException("listenerType",
                                                  listenerType,
                                                  "Unknown listener type");
        }


        public static string FormatKeyValuePairs(IDictionary<string, string> settings,
                                                 string valueSeparator = "=",
                                                 string parametersSeparator = ";")
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach( var item in settings )
            {
                if( stringBuilder.Length > 0 )
                    stringBuilder.Append(parametersSeparator);
                stringBuilder.Append(item.Key);
                stringBuilder.Append(valueSeparator);
                stringBuilder.Append(item.Value);
            }
            return stringBuilder.ToString();
        }


        private static string NullToEmpty(string s)
        {
            if( s == null )
                return string.Empty;
            return s;
        }
    }
}
