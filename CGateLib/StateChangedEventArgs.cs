using System;


namespace Mercatum.CGate
{
    public class StateChangedEventArgs : EventArgs
    {
        public IHavingCGateState Target { get; private set; }


        public StateChangedEventArgs(IHavingCGateState target)
        {
            Target = target;
        }
    }
}