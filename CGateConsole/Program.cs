using System;

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

            var target = new CGateConnectionTarget
                         {
                             Type = CGateConnectionType.Tcp,
                             Host = "127.0.0.1",
                             Port = 4001,
                             AppName = "CGateConsole"
                         };

            CGateConnection connection = new CGateConnection(target);
            Console.WriteLine("Connection object created.");

            //
            // Listener
            //

            var listener = new CGateReplicationListener(connection,
                                                        "FORTS_FUTINFO_REPL",
                                                        tables: new[] { "heartbeat" });

/*
            CGateReplicationListener listener = new CGateReplicationListener(connection,
                                                                             "FORTS_FUTTRADE_REPL",
                                                                             "ini/forts_scheme.ini",
                                                                             "FutTrade");
 */
/*
            CGateReplicationListener listener = new CGateReplicationListener(connection,
                                                                             "FORTS_FUTINFO_REPL",
                                                                             "ini/forts_scheme.ini",
                                                                             "FUTINFO");
 */
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
                    Console.WriteLine("{0}: New life num {1}.",
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
                        "{0}: clear revisions <= {1} for table {2}.",
                        lsn.StreamName,
                        eventArgs.Revision,
                        eventArgs.TableName);
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

            CGateStateMachine b = new CGateStateMachine(listener);
            b.CheckState();

            while( !exitRequested )
            {
                try
                {
                    connection.Process(1000);
                }
                catch( CGateException e )
                {
                    Console.WriteLine("CGate exception: {0}", e.ErrCode);
                    Console.WriteLine("{0}", e);
                    throw;
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
