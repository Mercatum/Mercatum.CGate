using System;
using System.Collections.Generic;
using System.Globalization;

using ru.micexrts.cgate;
using ru.micexrts.cgate.message;


namespace Mercatum.CGate
{
    public class CGatePublisher : IHavingCGateState, IDisposable
    {
        private bool _disposed;

        protected Publisher Publisher { get; set; }


        public State State
        {
            get
            {
                ErrorIfDisposed();
                return Publisher.State;
            }
        }


        public void Open()
        {
            ErrorIfDisposed();
            Publisher.Open();
        }


        public Message NewMessage(MessageKeyType type, object id)
        {
            ErrorIfDisposed();
            return Publisher.NewMessage(type, id);
        }


        public void Post(Message message, bool needReply)
        {
            ErrorIfDisposed();
            Publisher.Post(message, needReply ? PublishFlag.NeedReply : 0);
        }


        public void Close()
        {
            ErrorIfDisposed();
            Publisher.Close();
        }


        public void Dispose()
        {
            Dispose(true);
        }


        protected void ErrorIfDisposed()
        {
            if( _disposed )
                throw new ObjectDisposedException(GetType().Name);
        }


        private void Dispose(bool disposing)
        {
            if( !_disposed )
            {
                if( disposing )
                {
                    Publisher.Dispose();
                }

                _disposed = true;
            }
        }
    }


    public class CGateMQPublisher : CGatePublisher
    {
        public CGateMQPublisher(CGateConnection connection,
                                string name,
                                string service,
                                string category,
                                SchemeSource schemeSource,
                                uint timeout)
        {
            // TODO: check arguments validity

            if( connection == null )
                throw new ArgumentNullException("connection");

            string settings = FormatNewPublisherSettings(name,
                                                         service,
                                                         category,
                                                         schemeSource,
                                                         timeout);
            Publisher = new Publisher(connection.Handle, settings);
        }


        private string FormatNewPublisherSettings(string name,
                                                  string service,
                                                  string category,
                                                  SchemeSource schemeSource,
                                                  uint timeout)
        {
            if( service == null )
                service = string.Empty;

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            if( !string.IsNullOrEmpty(name) )
                parameters["name"] = name;

            if( !string.IsNullOrEmpty(category) )
                parameters["category"] = category;

            parameters["timeout"] = timeout.ToString(CultureInfo.InvariantCulture);
            parameters["scheme"] = CGateSettingsFormatter.FormatSchemeSource(schemeSource);

            return string.Format("p2mq://{0};{1}",
                                 service,
                                 CGateSettingsFormatter.FormatParameters(parameters));
        }
    }


    public class CGateFortsPublisher : CGateMQPublisher
    {
        public CGateFortsPublisher(CGateConnection connection,
                                   string name,
                                   SchemeSource schemeSource,
                                   uint timeout)
            : base(connection, name, "FORTS_SRV", "FORTS_MSG", schemeSource, timeout)
        {
        }
    }
}
