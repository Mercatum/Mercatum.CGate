using System;

using ru.micexrts.cgate;


namespace Mercatum.CGate
{
    /// <summary>
    /// Implements recommended strategy of managing state of cgate objects.
    /// </summary>
    public class CGateStateController
    {
        private State _previousState;
        private DateTime _previousOpenTime = DateTime.MinValue;
        private DateTime _nextOpenTime;

        /// <summary>
        /// Gets the object which state is controlled by this object.
        /// </summary>
        public IHavingCGateState Target { get; private set; }

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


        public CGateStateController(IHavingCGateState target)
        {
            if( target == null )
                throw new ArgumentNullException("target");

            Target = target;
            _previousState = target.State;

            ReopenTimeout = TimeSpan.FromSeconds(30);
            TooFastReopenThreshold = TimeSpan.FromSeconds(3);
        }


        /// <summary>
        /// Checks the object state.
        /// </summary>
        /// <remarks>
        /// CGate library allows objects to throw exceptions even in such cases when
        /// they should be handled by moving the object into the error state (for example,
        /// when a connection can't be established due to inaccessible router). There is no
        /// good way to separate such exceptions from "true" exceptions, thus all of them will
        /// be thrown to the user code. The application can handle them in any way it thinks
        /// it will be suitable and can continue to work the controller object.
        /// </remarks>
        public void CheckState()
        {
            State currentState = Target.State;

            if( currentState != _previousState )
            {
                var handler = StateChanged;
                if( handler != null )
                    handler(this, new StateChangedEventArgs(Target));

                _previousState = currentState;
            }

            switch( Target.State )
            {
            case State.Error:
                Target.Close();
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
                        try
                        {
                            Target.Open();
                        }
                        catch( CGateException e )
                        {
                            // When exception is thrown apply some rules as if the object goes into
                            // the error state.
                            _nextOpenTime = now + ReopenTimeout;
                            throw;
                        }
                        _previousOpenTime = now;
                    }
                }
                break;
            }
        }


        /// <summary>
        /// Checks the object state.
        /// </summary>
        /// <remarks>
        /// This version swallows all the exceptions and reports them using the CGate logging functions.
        /// It is recommended to use CheckState for development and switch to CheckStateSafe later (or use
        /// own exception handling).
        /// </remarks>
        public void CheckStateSafe()
        {
            try
            {
                CheckState();
            }
            catch( CGateException e )
            {
                CGateEnvironment.LogError("State check error: {0}", e);
            }
        }


        /// <summary>
        /// Returns a period of time when no activity is expected for the object.
        /// </summary>
        /// <returns>TimeSpan object representing the period of inactivity.</returns>
        public TimeSpan GetInactivityPeriod()
        {
            if( Target.State == State.Closed )
                return _nextOpenTime - DateTime.Now;
            return TimeSpan.Zero;
        }
    }
}
