using System;
using System.Collections.Generic;
using System.Globalization;

using ru.micexrts.cgate;


namespace Mercatum.CGate
{
    public class CGateConnection : IDisposable
    {
        private readonly Connection _connection;
        private bool _disposed;

        /// <summary>
        /// Gets current connection state.
        /// </summary>
        public State State
        {
            get
            {
                ErrorIfDisposed();
                return _connection.State;
            }
        }

        internal Connection Handle
        {
            get { return _connection; }
        }


        public CGateConnection(CGateConnectionTarget connectionTarget)
        {
            if( string.IsNullOrEmpty(connectionTarget.Host) )
                throw new InvalidOperationException("Host must be specified");

            if( connectionTarget.Port == 0 )
                throw new InvalidOperationException("Port number should be greater than zero");

            // TODO: is appName required for p2sys connections?
            if( string.IsNullOrEmpty(connectionTarget.AppName) )
                throw new InvalidOperationException("Application name should be specified");

            string settings = FormatNewConnectionSettings(connectionTarget);
            _connection = new Connection(settings);
        }


        public void Open()
        {
            ErrorIfDisposed();
            _connection.Open();
        }


        public void Process(TimeSpan timeout)
        {
            Process((uint)timeout.TotalMilliseconds);
        }


        public void Process(uint timeout)
        {
            ErrorIfDisposed();
            // TODO: analyze error code
            _connection.Process(timeout);
        }


        public void Close()
        {
            ErrorIfDisposed();
            _connection.Close();
        }


        public void Dispose()
        {
            Dispose(true);
        }


        private void ErrorIfDisposed()
        {
            if( _disposed )
                throw new ObjectDisposedException(typeof(CGateConnection).Name);
        }


        private void Dispose(bool disposing)
        {
            if( !_disposed )
            {
                if( disposing )
                {
                    _connection.Dispose();
                }

                _disposed = true;
            }
        }


        private static string FormatNewConnectionSettings(CGateConnectionTarget connectionTarget)
        {
            var settings = new Dictionary<string, string>();

            // TODO: ignore settings which are not used by the selected connection type

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
                                 CGateSettingsFormatter.FormatKeyValuePairs(settings));
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


        private static string NullToEmpty(string s)
        {
            if( s == null )
                return string.Empty;
            return s;
        }
    }
}
