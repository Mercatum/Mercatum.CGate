using System;


namespace Mercatum.CGate
{
    public class CGateConnectionTarget
    {
        public CGateConnectionType Type { get; private set; }

        public string Host { get; private set; }

        public ushort Port { get; private set; }

        /// <summary>
        /// Plaza-2 application name.
        /// </summary>
        /// <remarks>
        ///  Within one Plaza-2 router each connection to the router should have an unique name.
        /// </remarks>
        public string AppName { get; private set; }

        /// <summary>
        /// Password for connection with the Plaza-2 router, if the router is configured 
        /// to verify the connections to be opened.
        /// </summary>
        public string LocalPassword { get; private set; }

        /// <summary>
        /// Time in milliseconds spent on waiting for connection creation with the router 
        /// in the process of calling ‘conn_open(...)’. If this time exceeded 
        /// calling ‘conn_open(...)’ returns an error.
        /// </summary>
        public uint OpenTimeout { get; private set; }

        /// <summary>
        /// Object name in CGate.
        /// </summary>
        /// <remarks>
        /// UNSTABLE: what is this?
        /// </remarks>
        public string Name { get; private set; }

        /// <summary>
        /// Time in milliseconds spent on waiting for reply from the Plaza-2 router
        /// when the ‘p2lrpcq’ connection is used.
        /// </summary>
        public uint LrpcqTimeout { get; private set; }

        /// <summary>
        /// lrpcq buffer size (in bytes).
        /// </summary>
        /// <remarks>
        /// UNSTABLE: find out best default value.
        /// </remarks>
        public uint LrpcqBufferSize { get; private set; }


        public CGateConnectionTarget(CGateConnectionType type,
                                     string host,
                                     ushort port,
                                     string appName,
                                     string localPassword = "",
                                     uint openTimeout = 2000,
                                     string name = "",
                                     uint lrpcqTimeout = 500,
                                     uint lrpcqBufferSize = 16384)
        {
            if( type == CGateConnectionType.Lrpcq )
                host = "127.0.0.1";

            if( string.IsNullOrEmpty(host) )
                throw new ArgumentException("Host cannot be null or empty", "host");

            if( port == 0 )
                throw new ArgumentOutOfRangeException("port",
                                                      port,
                                                      "Port number should be greater than zero");

            // TODO: is appName required for p2sys connections?
            if( string.IsNullOrEmpty(appName) )
                throw new ArgumentException("Application name cannot be null or empty", "appName");

            Type = type;
            Host = host;
            Port = port;
            AppName = appName;
            LocalPassword = localPassword ?? string.Empty;
            OpenTimeout = openTimeout;
            Name = name ?? string.Empty;
            LrpcqTimeout = lrpcqTimeout;
            LrpcqBufferSize = lrpcqBufferSize;
        }

        // TODO: static methods for different connection types
    }
}
