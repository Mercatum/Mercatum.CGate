using Mercatum.CGate;


namespace Mercatum.CGateMonitor.Bridge
{
    class CGateDataStream
    {
        private readonly DataStreamSpec _spec;
        private CGateReplicationListener _listener;

        public DataStreamSpec Spec
        {
            get { return _spec; }
        }

        public CGateReplicationListener Listener
        {
            get { return _listener; }
            set { _listener = value; }
        }


        public CGateDataStream(DataStreamSpec spec)
        {
            _spec = spec;
        }
    }
}
