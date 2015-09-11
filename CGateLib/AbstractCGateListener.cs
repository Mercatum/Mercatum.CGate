using System;
using System.Collections.Generic;
using System.Globalization;

using ru.micexrts.cgate;
using ru.micexrts.cgate.message;


namespace Mercatum.CGate
{
    /// <summary>
    /// Common interface for different types of CGate listeners.
    /// </summary>
    public abstract class AbstractCGateListener : IDisposable
    {
        protected Listener Listener { get; set; }

        private bool _disposed;

        public State State
        {
            get
            {
                ErrorIfDisposed();
                return Listener == null ? State.Closed : Listener.State;
            }
        }

        public event EventHandler ListenerOpened;

        public event EventHandler<ListenerClosedEventArgs> ListenerClosed;


        public abstract void Open();


        public void Close()
        {
            ErrorIfDisposed();

            if( Listener != null )
                Listener.Close();
        }


        public void Dispose()
        {
            Dispose(true);
        }


        private void Dispose(bool disposing)
        {
            if( !_disposed )
            {
                if( disposing )
                {
                    Listener.Dispose();
                }

                _disposed = true;
            }
        }


        protected void ErrorIfDisposed()
        {
            if( _disposed )
                throw new ObjectDisposedException(GetType().Name);
        }


        /// <summary>
        /// Handle messages common for all listener types.
        /// </summary>
        /// <returns>true if the message has been handled, false otherwise.</returns>
        protected bool HandleCommonMessage(Message msg)
        {
            switch( msg.Type )
            {
            case MessageType.MsgOpen:
                // No additional information specified in the base message
                HandleOpenMessage(msg);
                return true;

            case MessageType.MsgClose:
                HandleCloseMessage(msg);
                return true;
            }

            return false;
        }


        protected virtual void HandleOpenMessage(Message baseMsg)
        {
            EventHandler handler = ListenerOpened;
            if( handler != null )
                handler(this, EventArgs.Empty);
        }


        protected virtual void HandleCloseMessage(Message baseMsg)
        {
            EventHandler<ListenerClosedEventArgs> handler = ListenerClosed;
            if( handler != null )
            {
                CloseMessage closeMsg = (CloseMessage)baseMsg;
                handler(this, new ListenerClosedEventArgs(closeMsg.CloseReason));
            }
        }
    }


    /// <summary>
    /// A listener receiving replication streams.
    /// </summary>
    public class CGateReplicationListener : AbstractCGateListener
    {
        public bool EnableSnapshotMode { get; set; }

        public bool EnableOnlineMode { get; set; }

        public bool IsOnline { get; private set; }

        public uint LifeNum { get; set; }

        public string ReplState { get; set; }

        public event EventHandler<DataArrivedEventArgs> DataArrived;

        public event EventHandler TransactionBegin;

        public event EventHandler TransactionCommitted;

        public event EventHandler SwitchedOnline;

        public event EventHandler<LifeNumChangedEventArgs> LifeNumChanged;

        public event EventHandler<DataClearedEventArgs> DataCleared;


        public CGateReplicationListener(CGateConnection connection,
                                        string streamName)
        {
            if( string.IsNullOrEmpty(streamName) )
                throw new ArgumentException("Stream name cannot be null or empty", "streamName");

            string settings =
                CGateSettingsFormatter.FormatNewListenerSettings(CGateListenerType.Replication,
                                                                 streamName);

            Listener = new Listener(connection.Handle, settings);
            Listener.Handler += HandleMessage;

            EnableSnapshotMode = true;
            EnableOnlineMode = true;
            LifeNum = 0;
            IsOnline = false;
        }


        public override void Open()
        {
            ErrorIfDisposed();

            string settings = FormatListenerOpenSettings();
            Listener.Open(settings);
        }


        private int HandleMessage(Connection conn,
                                  Listener listener,
                                  Message msg)
        {
            try
            {
                if( HandleCommonMessage(msg) )
                    return 0;

                switch( msg.Type )
                {
                case MessageType.MsgStreamData:
                    HandleStreamDataMessage(msg);
                    break;

                case MessageType.MsgTnBegin:
                    HandleTnBeginMessage(msg);
                    break;

                case MessageType.MsgTnCommit:
                    HandleTnCommitMessage(msg);
                    break;

                case MessageType.MsgP2ReplOnline:
                    HandleReplOnlineMessage(msg);
                    break;

                case MessageType.MsgP2ReplLifeNum:
                    HandleReplLifeNumMessage(msg);
                    break;

                case MessageType.MsgP2ReplClearDeleted:
                    HandleReplClearDeletedMessage(msg);
                    break;

                case MessageType.MsgP2ReplReplState:
                    HandleReplStateMessage(msg);
                    break;
                }

                return 0;
            }
            catch( Exception e )
            {
                CGateEnvironment.LogError("Unexpected error in HandleMessage: {0}", e);
                return 1;
            }
        }


        protected override void HandleCloseMessage(Message baseMsg)
        {
            IsOnline = false;

            base.HandleCloseMessage(baseMsg);
        }


        protected virtual void HandleStreamDataMessage(Message baseMsg)
        {
            EventHandler<DataArrivedEventArgs> handler = DataArrived;
            if( handler != null )
            {
                StreamDataMessage streamDataMessage = (StreamDataMessage)baseMsg;

                // TODO: keep scheme in DataArrivedEventArgs, expose tableName and `inserted` as properties there
                // to be calculated only when required
                SchemeDesc scheme = Listener.Scheme;
                MessageDesc messageDesc = scheme.Messages[streamDataMessage.MsgIndex];
                string tableName = messageDesc.Name;

                long replAct = streamDataMessage["replAct"].asLong();
                bool inserted = replAct == 0;

                handler(this, new DataArrivedEventArgs(tableName, inserted, streamDataMessage));
            }
        }


        protected virtual void HandleTnBeginMessage(Message baseMsg)
        {
            EventHandler handler = TransactionBegin;
            if( handler != null )
            {
                handler(this, EventArgs.Empty);
            }
        }


        protected virtual void HandleTnCommitMessage(Message msg)
        {
            EventHandler handler = TransactionCommitted;
            if( handler != null )
            {
                handler(this, EventArgs.Empty);
            }
        }


        protected virtual void HandleReplOnlineMessage(Message msg)
        {
            EventHandler handler = SwitchedOnline;
            if( handler != null )
            {
                handler(this, EventArgs.Empty);
            }
        }


        protected virtual void HandleReplLifeNumMessage(Message msg)
        {
            P2ReplLifeNumMessage lifeNumMessage = (P2ReplLifeNumMessage)msg;

            LifeNum = lifeNumMessage.LifeNumber;

            EventHandler<LifeNumChangedEventArgs> handler = LifeNumChanged;
            if( handler != null )
            {
                handler(this, new LifeNumChangedEventArgs(lifeNumMessage.LifeNumber));
            }
        }


        protected virtual void HandleReplClearDeletedMessage(Message msg)
        {
            P2ReplClearDeletedMessage clearDeletedMessage = (P2ReplClearDeletedMessage)msg;

            EventHandler<DataClearedEventArgs> handler = DataCleared;
            if( handler != null )
            {
                SchemeDesc scheme = Listener.Scheme;
                MessageDesc messageDesc = scheme.Messages[clearDeletedMessage.TableIdx];
                string tableName = messageDesc.Name;

                handler(this, new DataClearedEventArgs(tableName, clearDeletedMessage.TableRev));
            }
        }


        protected virtual void HandleReplStateMessage(Message msg)
        {
            P2ReplStateMessage replStateMessage = (P2ReplStateMessage)msg;

            ReplState = replStateMessage.ReplState;
        }


        private string FormatListenerOpenSettings()
        {
            const string SnapshotMode = "snapshot";
            const string OnlineMode = "online";

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            string mode = null;

            if( EnableSnapshotMode && EnableOnlineMode )
                mode = SnapshotMode + "+" + OnlineMode;
            else if( EnableSnapshotMode )
                mode = SnapshotMode;
            else if( EnableOnlineMode )
                mode = OnlineMode;

            if( !string.IsNullOrEmpty(mode) )
                parameters["mode"] = mode;

            if( !string.IsNullOrEmpty(ReplState) )
            {
                parameters["replstate"] = ReplState;
            }
            else
            {
                parameters["lifenum"] = LifeNum.ToString(CultureInfo.InvariantCulture);

                // TODO: revisions
            }

            return CGateSettingsFormatter.FormatKeyValuePairs(parameters);
        }
    }


    public class ListenerClosedEventArgs : EventArgs
    {
        public Reason CloseReason { get; private set; }


        public ListenerClosedEventArgs(Reason closeReason)
        {
            CloseReason = closeReason;
        }
    }


    public class DataArrivedEventArgs : EventArgs
    {
        public string TableName { get; private set; }

        // TODO: replace with ChangeType
        public bool Inserted { get; private set; }

        public AbstractDataMessage Message { get; private set; }


        public DataArrivedEventArgs(string tableName,
                                    bool inserted,
                                    AbstractDataMessage message)
        {
            TableName = tableName;
            Inserted = inserted;
            Message = message;
        }
    }


    public class LifeNumChangedEventArgs : EventArgs
    {
        public uint LifeNum { get; private set; }


        public LifeNumChangedEventArgs(uint lifeNum)
        {
            LifeNum = lifeNum;
        }
    }


    public class DataClearedEventArgs : EventArgs
    {
        public string TableName { get; private set; }

        public long Revision { get; private set; }


        public DataClearedEventArgs(string tableName,
                                    long revision)
        {
            TableName = tableName;
            Revision = revision;
        }
    }
}
