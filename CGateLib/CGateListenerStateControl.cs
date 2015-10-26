using System;

using ru.micexrts.cgate;


namespace Mercatum.CGate
{
    /// <summary>
    /// Implements recommended strategy of managing state of cgate objects.
    /// </summary>
    public class CGateStateMachine
    {
        private State _previousState;
        private DateTime _previousOpenTime = DateTime.MinValue;
        private DateTime _nextOpenTime;

        /// <summary>
        /// Gets the object which state is controlled by this object.
        /// </summary>
        public IHavingCGateState Object { get; private set; }

        /// <summary>
        /// Gets or sets timeout to wait before next attempt to open the object is made 
        /// in case if the previous attempt failed.
        /// </summary>
        /// <remarks>
        /// Setting the timeout to some reasonable value helps to avoid high CPU consumption when
        /// the object can't be opened right now.
        /// </remarks>
        public TimeSpan ReopenTimeout { get; set; }

        /// <summary>
        /// Gets or sets timeout to discover objects which are reopened too fast and 
        /// thus might be in the "invisible" error state.
        /// </summary>
        /// <remarks>
        /// It might happen that some object is closed immediately after opening because of some error
        /// but it doesn't enter the error state. To detect such situations objects which call Open() 
        /// before the timeout expires after previous successful Open() call are considered failed 
        /// and next Open() call will occur after <see cref="ReopenTimeout"/>.
        /// </remarks>
        public TimeSpan TooFastReopenThreshold { get; set; }


        public event EventHandler<StateChangedEventArgs> StateChanged;


        public CGateStateMachine(IHavingCGateState @object)
        {
            if( @object == null )
                throw new ArgumentNullException("object");

            Object = @object;
            _previousState = @object.State;

            ReopenTimeout = TimeSpan.FromSeconds(30);
            TooFastReopenThreshold = TimeSpan.FromSeconds(3);
        }


        public void CheckState()
        {
            State currentState = Object.State;

            if( currentState != _previousState )
            {
                var handler = StateChanged;
                if( handler != null )
                    handler(this, new StateChangedEventArgs(Object));

                _previousState = currentState;
            }

            switch( Object.State )
            {
            case State.Error:
                Object.Close();
                _nextOpenTime = DateTime.Now + ReopenTimeout;
                break;

            case State.Closed:
                DateTime now = DateTime.Now;
                if( _nextOpenTime <= now )
                {
                    if( now - _previousOpenTime <= TooFastReopenThreshold )
                    {
                        _nextOpenTime = now + ReopenTimeout;
                    }
                    else
                    {
                        // TODO: if an exception is thrown by Open should it be classified as faulted Open
                        // with timeout after?
                        Object.Open();
                        _previousOpenTime = now;
                    }
                }
                break;
            }
        }
    }


    public class StateChangedEventArgs : EventArgs
    {
        public IHavingCGateState Object { get; private set; }


        public StateChangedEventArgs(IHavingCGateState o)
        {
            Object = o;
        }
    }
}
