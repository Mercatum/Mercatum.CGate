using System;

using ru.micexrts.cgate;


namespace Mercatum.CGate
{
    public class ListenerClosedEventArgs : EventArgs
    {
        public Reason CloseReason { get; private set; }


        public ListenerClosedEventArgs(Reason closeReason)
        {
            CloseReason = closeReason;
        }
    }
}