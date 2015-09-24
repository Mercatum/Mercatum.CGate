using System;
using System.Collections.Generic;

using ru.micexrts.cgate;

// TODO: handling of exceptions
// 1. Ensure that connections, publishers and listeners are in the proper state if an exception is generated
//    and swallowed.

namespace Mercatum.CGate
{
    // TODO: better name
    public class CGateStateManager
    {
        private readonly CGateConnection _connection;
        private readonly List<ListenerHolder> _listeners = new List<ListenerHolder>();
        private readonly List<PublisherHolder> _publishers = new List<PublisherHolder>();

        private State _prevConnectionState;
        private DateTime _connectionOpenTime = DateTime.MinValue;


        /// <summary>
        /// Timeout used in calls to CGateConnection.Process().
        /// </summary>
        public TimeSpan ProcessMessagesTimeout { get; set; }

        /// <summary>
        /// Time to wait before opening of the connection if the connection was previously closed with an error.
        /// </summary>
        public TimeSpan ConnectionReopenTimeout { get; set; }

        /// <summary>
        /// Time to wait before opening of a listener if the listener was previously closed with an error.
        /// </summary>
        public TimeSpan ListenerReopenTimeout { get; set; }

        /// <summary>
        /// If a listener is opened and then closed again without good reason in the specified timeout then 
        /// it is considered as broken. Next reopen occurs after <see cref="ListenerReopenTimeout"/>.
        /// </summary>
        /// <remarks>
        /// In some cases listeners can be closed due to errors but they don't enter Error state. Detecting 
        /// listeners which are reopened too fast helps to avoid this problem.
        /// </remarks>
        public TimeSpan TooFastListenerReopenThreshold { get; set; }

        /// <summary>
        /// Time to wait before opening of a publisher if the publisher was previously closed with an error.
        /// </summary>
        public TimeSpan PublisherReopenTimeout { get; set; }


        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        public event EventHandler<ListenerStateChangedEventArgs> ListenerStateChanged;

        public event EventHandler<PublisherStateChangedEventArgs> PublisherStateChanged;


        public CGateStateManager(CGateConnection connection)
        {
            // Default values for properties
            ProcessMessagesTimeout = TimeSpan.FromMilliseconds(500);
            ConnectionReopenTimeout = TimeSpan.FromSeconds(30);
            PublisherReopenTimeout = TimeSpan.FromSeconds(30);
            ListenerReopenTimeout = TimeSpan.FromSeconds(30);
            TooFastListenerReopenThreshold = TimeSpan.FromSeconds(5);

            _connection = connection;
            _prevConnectionState = connection.State;
        }


        public void AddListener(AbstractCGateListener listener)
        {
            _listeners.Add(new ListenerHolder(listener));
        }


        public void AddPublisher(AbstractCGatePublisher publisher)
        {
            _publishers.Add(new PublisherHolder(publisher));
        }


        public void Perform()
        {
            CheckConnectionState();

            State connectionState = _connection.State;

            // TODO: potential high CPU load when not connected to the router
            if( connectionState == State.Active )
            {
                CheckPublishersState();
                CheckListenersState();
            }

            if( connectionState == State.Active || connectionState == State.Opening )
                _connection.Process(ProcessMessagesTimeout);
        }


        private void CheckConnectionState()
        {
            State currentState = _connection.State;

            if( currentState != _prevConnectionState )
            {
                _prevConnectionState = currentState;
                var handler = ConnectionStateChanged;
                if( handler != null )
                    handler(this, new ConnectionStateChangedEventArgs(_connection));
            }

            switch( currentState )
            {
            case State.Error:
                _connection.Close();
                _connectionOpenTime = DateTime.Now + ConnectionReopenTimeout;
                break;

            case State.Closed:
                if( _connectionOpenTime <= DateTime.Now )
                    _connection.Open();
                break;
            }
        }


        private void CheckPublishersState()
        {
            // TODO: see todo in CheckListenersState
            foreach( PublisherHolder holder in _publishers )
            {
                State currentState = holder.Publisher.State;

                if( currentState != holder.PreviousState )
                {
                    holder.PreviousState = currentState;
                    var handler = PublisherStateChanged;
                    if( handler != null )
                        handler(this, new PublisherStateChangedEventArgs(holder.Publisher));
                }

                switch( currentState )
                {
                case State.Error:
                    holder.NextOpenTime = DateTime.Now + ListenerReopenTimeout;
                    holder.Publisher.Close();
                    break;

                case State.Closed:
                    DateTime now = DateTime.Now;
                    if( holder.NextOpenTime <= DateTime.Now )
                    {
                        if( now - holder.LastOpenedTime <= TooFastListenerReopenThreshold )
                        {
                            holder.NextOpenTime = now + ListenerReopenTimeout;
                        }
                        else
                        {
                            holder.Publisher.Open();
                            holder.LastOpenedTime = now;
                        }
                    }
                    break;
                }
            }
        }


        private void CheckListenersState()
        {
            // TODO: if some listeners permanently throw exceptions during Open/Close calls,
            // other listeners might stay closed (the foreach loop breaks on the exceptions)
            foreach( ListenerHolder holder in _listeners )
            {
                State currentState = holder.Listener.State;

                if( currentState != holder.PreviousState )
                {
                    holder.PreviousState = currentState;
                    var handler = ListenerStateChanged;
                    if( handler != null )
                        handler(this, new ListenerStateChangedEventArgs(holder.Listener));
                }

                switch( currentState )
                {
                case State.Error:
                    holder.NextOpenTime = DateTime.Now + ListenerReopenTimeout;
                    holder.Listener.Close();
                    break;

                case State.Closed:
                    DateTime now = DateTime.Now;
                    // TODO: there is no need to check the state for listeners with scheduled reopen.
                    // TODO: Keep them in a priority queue, this also solves the issue with exceptions (see above).
                    if( holder.NextOpenTime <= DateTime.Now )
                    {
                        if( now - holder.LastOpenedTime <= TooFastListenerReopenThreshold )
                        {
                            holder.NextOpenTime = now + ListenerReopenTimeout;
                        }
                        else
                        {
                            holder.Listener.Open();
                            holder.LastOpenedTime = now;
                        }
                    }
                    break;
                }
            }
        }


        private class ListenerHolder
        {
            public AbstractCGateListener Listener { get; private set; }

            public State PreviousState { get; set; }

            public DateTime LastOpenedTime { get; set; }

            public DateTime NextOpenTime { get; set; }


            public ListenerHolder(AbstractCGateListener listener)
            {
                Listener = listener;
                PreviousState = listener.State;
                LastOpenedTime = DateTime.MinValue;
                NextOpenTime = DateTime.MinValue;
            }
        }


        private class PublisherHolder
        {
            public AbstractCGatePublisher Publisher { get; private set; }

            public State PreviousState { get; set; }

            public DateTime LastOpenedTime { get; set; }

            public DateTime NextOpenTime { get; set; }


            public PublisherHolder(AbstractCGatePublisher publisher)
            {
                Publisher = publisher;
                PreviousState = publisher.State;
                LastOpenedTime = DateTime.MinValue;
                NextOpenTime = DateTime.MinValue;
            }
        }
    }


    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public CGateConnection Connection { get; private set; }


        public ConnectionStateChangedEventArgs(CGateConnection connection)
        {
            Connection = connection;
        }
    }


    public class ListenerStateChangedEventArgs : EventArgs
    {
        public AbstractCGateListener Listener { get; private set; }


        public ListenerStateChangedEventArgs(AbstractCGateListener listener)
        {
            Listener = listener;
        }
    }


    public class PublisherStateChangedEventArgs : EventArgs
    {
        public AbstractCGatePublisher Publisher { get; private set; }


        public PublisherStateChangedEventArgs(AbstractCGatePublisher publisher)
        {
            Publisher = publisher;
        }
    }
}
