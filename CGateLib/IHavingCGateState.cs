using ru.micexrts.cgate;


namespace Mercatum.CGate
{
    /// <summary>
    /// Represents an object which has a state in the CGate style.
    /// </summary>
    public interface IHavingCGateState
    {
        /// <summary>
        /// Returns current state of the object.
        /// </summary>
        State State { get; }

        /// <summary>
        /// Opens the object.
        /// </summary>
        void Open();

        /// <summary>
        /// Closes the object.
        /// </summary>
        void Close();
    }
}
