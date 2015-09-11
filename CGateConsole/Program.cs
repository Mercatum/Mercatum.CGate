using System;
using System.Threading;

using Mercatum.CGate;


namespace CGateConsole
{
    /// <summary>
    /// This program shows basic usage patterns for the CGate library.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
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

            CGateEnvironment.Init();
            Console.WriteLine("CGate environment initialized.");

            //
            // Connection establishing
            //

            var target = new CGateConnectionTarget()
                         {
                             Type = CGateConnectionType.Tcp,
                             Host = "127.0.0.1",
                             Port = 4001,
                             AppName = "CGateConsole"
                         };

            CGateConnection connection = new CGateConnection(target);
            Console.WriteLine("Connection object created.");

            connection.Open();
            Console.WriteLine("Connection established.");

            while( !exitRequested )
            {
                connection.Process(500);    
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
