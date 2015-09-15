using System;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

namespace PlugNPayClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!Environment.UserInteractive)
            {
                ServiceBase.Run(new PlugNPlayService());
                return;
            }

            string parameter = string.Concat(args);
            switch (parameter)
            {
                case "-i":
                    ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                    break;

                case "-u":
                    ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
                    break;

                case "-s":
                    {
                        try
                        {
                            PlugNPlayService ctx = new PlugNPlayService();
                            ctx.Start();

                            do
                            {
                                Console.WriteLine("Press ESC to exit...");
                            }
                            while (Console.ReadKey().Key != ConsoleKey.Escape);

                            ctx.Stop();
                        }
                        catch(Exception ex)
                        {
                            while (Console.ReadKey().Key != ConsoleKey.Escape) { }
                        }
                        break;
                    }

                default:
                    {
                        goto case "-s";

                        Console.WriteLine("Provide start parameters:");
                        Console.WriteLine("[-s] Start in console mode");
                        Console.WriteLine("[-i] Install service");
                        Console.WriteLine("[-u] Uninstall service");
                        break;
                    }
            }
        }
    }
}
