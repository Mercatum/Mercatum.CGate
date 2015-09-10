using System;

using ru.micexrts.cgate;


namespace Mercatum.CGate
{
    public class CGateConnection : IDisposable
    {
        private readonly Connection _connection;
        private bool _disposed;


        public State State
        {
            get
            {
                ErrorIfDisposed();
                return _connection.State;
            }
        }

        internal Connection Handle
        {
            get { return _connection; }
        }


        public CGateConnection(CGateConnectionTarget connectionTarget)
        {
            string settings = CGateSettingsFormatter.FormatNewConnectionSettings(connectionTarget);
            _connection = new Connection(settings);
        }


        public void Open()
        {
            ErrorIfDisposed();
            _connection.Open();
        }


        public void Process(TimeSpan timeout)
        {
            Process((uint)timeout.TotalMilliseconds);
        }


        public void Process(uint timeout)
        {
            ErrorIfDisposed();
            // TODO: analyze error code
            _connection.Process(timeout);
        }


        public void Close()
        {
            ErrorIfDisposed();
            _connection.Close();
        }


        public void Dispose()
        {
            Dispose(true);
        }


        private void ErrorIfDisposed()
        {
            if( _disposed )
                throw new ObjectDisposedException(typeof(CGateConnection).Name);
        }


        private void Dispose(bool disposing)
        {
            if( !_disposed )
            {
                if( disposing )
                {
                    _connection.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
