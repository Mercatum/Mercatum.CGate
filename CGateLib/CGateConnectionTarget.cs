using System;


namespace Mercatum.CGate
{
    public class CGateConnectionTarget
    {
        /// <summary>
        /// Gets or sets the connection type.
        /// </summary>
        public CGateConnectionType Type { get; set; }

        /// <summary>
        /// Gets or sets destination host of the connection (the host where the router is located).
        /// For lrpcq connection type should be always "127.0.0.1" or empty (in this case "127.0.0.1"
        /// will be used by default).
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the port which is used to create connection. It should be specified for both 
        /// tcp and lrpcq connection types; in the last case the port will be used as a control channel 
        /// for connection creation via shared memory.
        /// </summary>
        public ushort Port { get; set; }

        /// <summary>
        /// Plaza-2 application name.
        /// </summary>
        /// <remarks>
        ///  Within one Plaza-2 router each connection to the router should have an unique name.
        /// </remarks>
        public string AppName { get; set; }

        /// <summary>
        /// Password for connection with the Plaza-2 router, if the router is configured 
        /// to verify the connections to be opened.
        /// </summary>
        public string LocalPassword { get; set; }

        /// <summary>
        /// Time in milliseconds spent on waiting for connection creation with the router 
        /// in the process of calling ‘conn_open(...)’. If this time exceeded 
        /// calling ‘conn_open(...)’ returns an error.
        /// </summary>
        public uint OpenTimeout { get; set; }

        /// <summary>
        /// Time in milliseconds spent on waiting for reply from the Plaza-2 router
        /// when the ‘p2lrpcq’ connection is used.
        /// </summary>
        public uint LrpcqTimeout { get; set; }

        /// <summary>
        /// lrpcq buffer size (in bytes).
        /// </summary>
        /// <remarks>
        /// UNSTABLE: find out best default value.
        /// </remarks>
        public uint LrpcqBufferSize { get; set; }

        /// <summary>
        /// Object name in CGate.
        /// </summary>
        /// <remarks>
        /// UNSTABLE: what is this?
        /// </remarks>
        public string Name { get; set; }


        public CGateConnectionTarget()
        {
            // Set up reasonable default values
            OpenTimeout = 5000;
            LrpcqTimeout = 5000;
            LrpcqBufferSize = 16384;
        }
    }
}
