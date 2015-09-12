using System;
using System.Collections.Generic;

using ru.micexrts.cgate;


namespace Mercatum.CGate
{
    public class CGateStateManager
    {
        private readonly CGateConnection _connection;
        private readonly List<ListenerHolder> _listeners = new List<ListenerHolder>();

        private TimeSpan _processMessagesTimeout = TimeSpan.FromMilliseconds(500);
        private TimeSpan _connectionReopenTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _listenerReopenTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _tooFastListenerReopenThreshold = TimeSpan.FromSeconds(5);

        private DateTime _connectionOpenTime = DateTime.MinValue;

        public TimeSpan ProcessMessagesTimeout
        {
            get { return _processMessagesTimeout; }
            set { _processMessagesTimeout = value; }
        }


        public CGateStateManager(CGateConnection connection)
        {
            _connection = connection;
        }


        public void AddListener(AbstractCGateListener listener)
        {
            _listeners.Add(new ListenerHolder(listener));
        }


        public void Perform()
        {
            CheckConnectionState();

            State connectionState = _connection.State;
            
            // TODO: potential high CPU load when not connected to the router
            if( connectionState == State.Active )
            {
                CheckListenersState();
            }

            if( connectionState == State.Active || connectionState == State.Opening )
                _connection.Process(_processMessagesTimeout);
        }


        private void CheckConnectionState()
        {
            switch( _connection.State )
            {
            case State.Error:
                _connection.Close();
                _connectionOpenTime = DateTime.Now + _connectionReopenTimeout;
                break;

            case State.Closed:
                if( _connectionOpenTime <= DateTime.Now )
                    _connection.Open();
                break;
            }
        }


        private void CheckListenersState()
        {
            // TODO: if some listeners permanently throw exceptions during Open/Close calls,
            // other listeners might stay closed (the foreach loop breaks on the exceptions)
            foreach( ListenerHolder holder in _listeners )
            {
                switch( holder.Listener.State )
                {
                case State.Error:
                    holder.NextOpenTime = DateTime.Now + _listenerReopenTimeout;
                    holder.Listener.Close();
                    break;

                case State.Closed:
                    DateTime now = DateTime.Now;
                    if( holder.NextOpenTime <= DateTime.Now )
                    {
                        if( now - holder.LastOpenedTime <= _tooFastListenerReopenThreshold )
                        {
                            holder.NextOpenTime = now + _listenerReopenTimeout;
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
            private readonly AbstractCGateListener _listener;

            public AbstractCGateListener Listener
            {
                get { return _listener; }
            }

            public DateTime LastOpenedTime { get; set; }
            public DateTime NextOpenTime { get; set; }

            public ListenerHolder(AbstractCGateListener listener)
            {
                _listener = listener;
                LastOpenedTime = DateTime.MinValue;
                NextOpenTime = DateTime.MinValue;
            }
        }
    }
}
