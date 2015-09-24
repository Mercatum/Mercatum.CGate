using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Mercatum.CGate;


namespace Mercatum.CGateMonitor.Bridge
{
    internal class CGateBridge
    {
        private readonly List<DataStreamSpec> _availableDataStreams = new List<DataStreamSpec>();

        private CancellationTokenSource _cancellationTokenSource;
        private Task _dataExchangeTask;
        private CGateDataStream _stream;

        public List<DataStreamSpec> AvailableDataStreams
        {
            get { return _availableDataStreams; }
        }


        public ICommand ConnectToCGate { get; set; }


        public CGateBridge()
        {
            _availableDataStreams.Add(new DataStreamSpec("FORTS_FUTINFO_REPL",
                                                         "FUTINFO",
                                                         "delivery_report",
                                                         "fut_rejected_orders",
                                                         "fut_intercl_info",
                                                         "fut_bond_registry",
                                                         "fut_bond_isin",
                                                         "fut_bond_nkd",
                                                         "fut_bond_nominal",
                                                         "usd_online",
                                                         "fut_vcb",
                                                         "session",
                                                         "multileg_dict",
                                                         "fut_sess_contents",
                                                         "fut_instruments",
                                                         "diler",
                                                         "investr",
                                                         "fut_sess_settl",
                                                         "sys_messages",
                                                         "prohibition",
                                                         "rates",
                                                         "sys_events"));

            _availableDataStreams.Add(new DataStreamSpec("FORTS_FUTTRADE_REPL",
                                                         "FutTrade",
                                                         "orders_log",
                                                         "multileg_orders_log",
                                                         "deal",
                                                         "multileg_deal"));

            _availableDataStreams.Add(new DataStreamSpec("FORTS_OPTTRADE_REPL", "OptTrade"));

            // create a stream for FORTS_FUTINFO_REPL for testing
            _stream = new CGateDataStream(_availableDataStreams[0]);

            ConnectToCGate = new RelayCommand(PerformConnectToCGate);
        }


        public void Init()
        {
            CGateEnvironment.Initialize();
        }



        public void Close()
        {
            if( CGateEnvironment.Initialized )
                CGateEnvironment.Close();
        }


        private void PerformConnectToCGate(object parameter)
        {
            if( !CGateEnvironment.Initialized )
                Init();

            if( _dataExchangeTask != null )
                return;

            _cancellationTokenSource = new CancellationTokenSource();

            _dataExchangeTask = Task.Factory.StartNew(CGateDataExchangeWorker,
                                                      _cancellationTokenSource.Token,
                                                      TaskCreationOptions.LongRunning);
        }


        private void CGateDataExchangeWorker(object parameter)
        {
            var target = new CGateConnectionTarget
                         {
                             Type = CGateConnectionType.Tcp,
                             Host = "127.0.0.1",
                             Port = 4001,
                             AppName = "cgate_monitor"
                         };

            CGateConnection connection = new CGateConnection(target);
            CGateStateManager exchange = new CGateStateManager(connection);

            _stream.Listener = new CGateReplicationListener(connection, _stream.Spec.StreamName, new SchemeSource("todo", "todo"));
            exchange.AddListener(_stream.Listener);

            while( !_cancellationTokenSource.Token.IsCancellationRequested )
            {
                exchange.Perform();
            }

            connection.Close();
        }
    }
}
