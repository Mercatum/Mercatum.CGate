using System;
using System.Threading;

using Mercatum.CGate;

using ru.micexrts.cgate;


namespace CGateConsole
{
    /// <summary>
    /// This program shows basic usage patterns for the CGate library.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            string streamName = "FORTS_FUTINFO_REPL";
            string[] tables = { "heartbeat" }; // use null to access all possible tables

            // How to connect to the router
            var target = new CGateConnectionTarget
                         {
                             Type = CGateConnectionType.Tcp,
                             Host = "127.0.0.1",
                             Port = 4010,
                             AppName = "CGateConsole"
                         };

            // Exit the main loop when Ctrl+C is pressed
            bool exitRequested = false;
            Console.CancelKeyPress += (sender,
                                       eventArgs) =>
                                      {
                                          exitRequested = true;
                                          eventArgs.Cancel = true;
                                          Console.WriteLine("Ctrl+C pressed.");
                                      };

            //
            // Environment initialization
            //

            CGateEnvironment.IniPath = @"ini\cgate.ini";
            CGateEnvironment.ClientKey = CGateEnvironment.TestClientKey;
            CGateEnvironment.InitializeMq = true;
            CGateEnvironment.InitializeReplClient = true;
            // TODO: what is the difference between p2 and p2:p2syslog?
            // Plain p2 mode doesn't log everything.
            CGateEnvironment.LogMode = CGateLogMode.P2;
            CGateEnvironment.LogSettingsSection = "p2syslog";

            CGateEnvironment.Open();
            Console.WriteLine("CGate environment initialized.");

            //
            // Connection
            //

            CGateConnection connection = new CGateConnection(target);
            Console.WriteLine("Connection object created.");

            //
            // Listener
            //

            var listener = new CGateReplicationListener(connection,
                                                        streamName,
                                                        tables: tables);

            listener.ListenerOpened +=
                (sender, eventArgs) =>
                {
                    CGateReplicationListener lsn = (CGateReplicationListener)sender;
                    Console.WriteLine("{0}: opened.", lsn.StreamName);
                };

            listener.ListenerClosed +=
                (sender, eventArgs) =>
                {
                    CGateReplicationListener lsn = (CGateReplicationListener)sender;
                    string replState = lsn.ReplState;
                    Console.WriteLine("{0}: closed; reason {1}; replstate {2}.",
                                      lsn.StreamName,
                                      eventArgs.CloseReason,
                                      replState == null
                                          ? "(null)"
                                          : replState.Length.ToString() + " characters");
                };

            listener.LifeNumChanged +=
                (sender, eventArgs) =>
                {
                    CGateReplicationListener lsn = (CGateReplicationListener)sender;
                    Console.WriteLine("{0}: life num {1}.",
                                      lsn.StreamName,
                                      eventArgs.LifeNum);
                };

            listener.SwitchedOnline +=
                (sender, eventArgs) =>
                {
                    CGateReplicationListener lsn = (CGateReplicationListener)sender;
                    Console.WriteLine("{0}: online.", lsn.StreamName);
                };

            listener.DataCleared +=
                (sender, eventArgs) =>
                {
                    CGateReplicationListener lsn = (CGateReplicationListener)sender;
                    Console.WriteLine(
                        "{0}: {1}: delete revisions <= {2}.",
                        lsn.StreamName,
                        eventArgs.TableName,
                        eventArgs.Revision);
                };

            listener.TransactionBegin +=
                (sender, eventArgs) =>
                {
                    CGateReplicationListener lsn = (CGateReplicationListener)sender;
                    Console.WriteLine("{0}: tx begin.", lsn.StreamName);
                };

            listener.TransactionCommitted +=
                (sender, eventArgs) =>
                {
                    CGateReplicationListener lsn = (CGateReplicationListener)sender;
                    Console.WriteLine("{0}: tx commit.", lsn.StreamName);
                };

            listener.DataArrived +=
                (sender, eventArgs) =>
                {
                    CGateReplicationListener lsn = (CGateReplicationListener)sender;

                    if( !lsn.IsOnline )
                        return;

                    Console.WriteLine("{0}/{1}: {2} data; rev {3}.\n{4}",
                                      lsn.StreamName,
                                      eventArgs.TableName,
                                      eventArgs.Inserted ? "+" : "-",
                                      eventArgs.Revision,
                                      eventArgs.Message
                        );
                };

            //
            // State manager and main loop
            //

            CGateStateController connectionStateController = new CGateStateController(connection);
            connectionStateController.StateChanged +=
                (sender, eventArgs) =>
                {
                    Console.WriteLine("Connection changed state to {0}", eventArgs.Target.State);
                };

            CGateStateController listenerStateController = new CGateStateController(listener);
            listenerStateController.StateChanged +=
                (sender, eventArgs) =>
                {
                    Console.WriteLine("Listener changed state to {0}", eventArgs.Target.State);
                };

            while( !exitRequested )
            {
                try
                {
                    connectionStateController.CheckState();
                    TimeSpan inactivityPeriod = connectionStateController.GetInactivityPeriod();
                    if( inactivityPeriod != TimeSpan.Zero )
                        Thread.Sleep(inactivityPeriod);

                    if( connection.State == State.Active )
                        listenerStateController.CheckState();

                    if( connection.State == State.Opening || connection.State == State.Active )
                        connection.Process(1000);
                }
                catch( CGateException e )
                {
                    Console.WriteLine("CGate exception: {0}", e.ErrCode);
                    Console.WriteLine("{0}", e);
                }
            }

            //
            // Closing resources
            //

            Console.WriteLine("Closing resources.");

            connection.Close();
            CGateEnvironment.Close();
        }
    }
}
