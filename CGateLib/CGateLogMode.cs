namespace Mercatum.CGate
{
    /// <summary>
    /// Available logging modes.
    /// </summary>
    /// <remarks>
    /// These logging modes are used by the CGate library internally to output its own internal information.
    /// If the application wants to use its own logging it should implement it by itself.
    /// </remarks>
    public enum CGateLogMode
    {
        /// <summary>
        /// Output logs into /dev/null.
        /// </summary>
        Null,

        /// <summary>
        /// Output cgate logs into stdout.
        /// </summary>
        Std,

        /// <summary>
        /// Output logs according to settings in the configuration file.
        /// TODO: rename to something more meaningful?
        /// </summary>
        P2
    }
}
