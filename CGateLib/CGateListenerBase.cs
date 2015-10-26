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
    public abstract class CGateListenerBase : IHavingCGateState, IDisposable
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


        protected virtual void HandleCloseMessage(Message msg)
        {
            EventHandler<ListenerClosedEventArgs> handler = ListenerClosed;
            if( handler != null )
            {
                CloseMessage closeMsg = (CloseMessage)msg;
                handler(this, new ListenerClosedEventArgs(closeMsg.CloseReason));
            }
        }
    }


    /// <summary>
    /// A listener receiving replication streams.
    /// </summary>
    public class CGateReplicationListener : CGateListenerBase
    {
        private ICollection<string> _tables;

        public string StreamName { get; private set; }

        public bool IsOnline { get; private set; }

        public bool EnableSnapshotMode { get; set; }

        public bool EnableOnlineMode { get; set; }

        public IDictionary<string, long> Revisions { get; set; }

        public uint LifeNum { get; set; }

        public string ReplState { get; set; }

        public event EventHandler<DataArrivedEventArgs> DataArrived;

        public event EventHandler TransactionBegin;

        public event EventHandler TransactionCommitted;

        public event EventHandler SwitchedOnline;

        public event EventHandler<LifeNumChangedEventArgs> LifeNumChanged;

        public event EventHandler<DataClearedEventArgs> DataCleared;


        public CGateReplicationListener(CGateConnection connection,
                                        string streamName,
                                        SchemeSource schemeSource = null,
                                        ICollection<string> tables = null)
        {
            if( string.IsNullOrEmpty(streamName) )
                throw new ArgumentException("Stream name cannot be null or empty", "streamName");

            string settings = FormatListenerNewSettings(streamName, schemeSource, tables);

            Listener = new Listener(connection.Handle, settings) { Handler = HandleMessage };

            _tables = tables;

            StreamName = streamName;
            IsOnline = false;

            EnableSnapshotMode = true;
            EnableOnlineMode = true;
            LifeNum = 0;
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


        protected override void HandleCloseMessage(Message msg)
        {
            IsOnline = false;

            base.HandleCloseMessage(msg);
        }


        protected virtual void HandleStreamDataMessage(Message msg)
        {
            EventHandler<DataArrivedEventArgs> handler = DataArrived;
            if( handler != null )
            {
                StreamDataMessage streamDataMessage = (StreamDataMessage)msg;

                // TODO: keep scheme in DataArrivedEventArgs, expose tableName and `inserted` as properties there
                // to be calculated only when required
                SchemeDesc scheme = Listener.Scheme;
                MessageDesc messageDesc = scheme.Messages[streamDataMessage.MsgIndex];
                string tableName = messageDesc.Name;

                long replAct = streamDataMessage["replAct"].asLong();
                bool inserted = replAct == 0;

                handler(this,
                        new DataArrivedEventArgs(tableName,
                                                 streamDataMessage.Rev,
                                                 inserted,
                                                 streamDataMessage));
            }
        }


        protected virtual void HandleTnBeginMessage(Message msg)
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
            IsOnline = true;

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


        private static string FormatListenerNewSettings(string streamName,
                                                        SchemeSource schemeSource,
                                                        ICollection<string> tables)
        {
            if( streamName == null )
                streamName = string.Empty;

            string parameters = string.Empty;

            if( schemeSource != null )
                parameters = string.Format("scheme={0}",
                                           CGateSettingsFormatter.FormatSchemeSource(schemeSource));
            else if( tables != null )
            {
                // TODO: investigate a case when tables.Length == 0
                parameters = "tables=" + string.Join(",", tables);
            }

            return string.Format("p2repl://{0};{1}",
                                 streamName,
                                 parameters);
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
                // TODO: do revisions make sense when lifenum=0?
                parameters["lifenum"] = LifeNum.ToString(CultureInfo.InvariantCulture);

                if( Revisions != null )
                {
                    foreach( var entry in Revisions )
                    {
                        // TODO: filter by tables?
                        parameters["rev." + entry.Key] =
                            entry.Value.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            return CGateSettingsFormatter.FormatParameters(parameters);
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

        public long Revision { get; private set; }

        // TODO: replace with ChangeType
        public bool Inserted { get; private set; }

        public AbstractDataMessage Message { get; private set; }


        public DataArrivedEventArgs(string tableName,
                                    long revision,
                                    bool inserted,
                                    AbstractDataMessage message)
        {
            TableName = tableName;
            Revision = revision;
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
