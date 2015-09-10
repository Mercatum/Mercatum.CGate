namespace Mercatum.CGate
{
    public enum CGateConnectionType
    {
        /// <summary>
        /// Connection to the Plaza-2 router via the TCP/IP protocol.
        /// </summary>
        Tcp,

        /// <summary>
        /// Connection to the Plaza-2 router via shared memory.
        /// </summary>
        Lrpcq,

        /// <summary>
        /// A special connection type which allows to manage router.
        /// </summary>
        Sys
    }
}
