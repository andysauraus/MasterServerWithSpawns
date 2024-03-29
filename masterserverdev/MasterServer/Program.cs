﻿using BeardedManStudios;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace MasterServer
{
	class Program
	{
		private static void Main(string[] args)
		{
			string host = "0.0.0.0";
			ushort port = 15940;
			string read = string.Empty;
			int eloRange = 0;
            int serverCount = 0;
            string unityPath = "";

			Dictionary<string, string> arguments = ArgumentParser.Parse(args);
            
            if (args.Length > 0)
			{
				if (arguments.ContainsKey("host"))
					host = arguments["host"];

				if (arguments.ContainsKey("port"))
					ushort.TryParse(arguments["port"], out port);

				if (arguments.ContainsKey("elorange"))
					int.TryParse(arguments["elorange"], out eloRange);
                if (arguments.ContainsKey("-servercount"))
                {
                    int.TryParse(arguments["-servercount"], out serverCount);
                }
                if (arguments.ContainsKey("-unitypath"))
                {
                    unityPath = arguments["-unitypath"];
                }
			}
			else
			{
				Console.WriteLine("Entering nothing will choose defaults.");
				Console.WriteLine("Enter Host IP (Default: "+ GetLocalIPAddress() + "):");
				read = Console.ReadLine();
				if (string.IsNullOrEmpty(read))
					host = GetLocalIPAddress();
				else
					host = read;

				Console.WriteLine("Enter Port (Default: 15940):");
				read = Console.ReadLine();
				if (string.IsNullOrEmpty(read))
					port = 15940;
				else
				{
					ushort.TryParse(read, out port);
				}
			}

			Console.WriteLine(string.Format("Hosting ip [{0}] on port [{1}]", host, port));
			PrintHelp();
			MasterServer server = new MasterServer(host, port);
			server.EloRange = eloRange;
			server.ToggleLogging();

            UnityManager unityManager = new UnityManager();
            Console.WriteLine(string.Format("starting [{0}] servers for [{1}] on port [{2}]", serverCount, host, port));
            unityManager.StartSpawning(serverCount, unityPath, port);

			while (true)
			{
                unityManager.UpdateServers();

				read = Console.ReadLine().ToLower();
				if (read == "s" || read == "stop")
				{
					lock (server)
					{
						Console.WriteLine("Server stopped.");
						server.Dispose();
					}
				}
				else if (read == "l" || read == "log")
				{
					if (server.ToggleLogging())
						Console.WriteLine("Logging has been enabled");
					else
						Console.WriteLine("Logging has been disabled");
				}
				else if (read == "r" || read == "restart")
				{
					lock (server)
					{
						if (server.IsRunning)
						{
							Console.WriteLine("Server stopped.");
							server.Dispose();
						}
					}
                    lock (unityManager)
                    {
                        System.Threading.Tasks.Task task = new System.Threading.Tasks.Task(() =>
                        {
                            unityManager.KillUnityInstances();
                            unityManager.UpdateServers();
                        });
                        task.Start();
                    }

					Console.WriteLine("Restarting...");
					Console.WriteLine(string.Format("Hosting ip [{0}] on port [{1}]", host, port));
					server = new MasterServer(host, port);
				}
				else if (read == "q" || read == "quit")
				{
                    lock (unityManager)
                    {
                        //System.Threading.Tasks.Task task = new System.Threading.Tasks.Task(() =>
                        //{
                            unityManager.KillUnityInstances();
                        //});
                        //task.RunSynchronously();
                    }
					lock (server)
					{
						Console.WriteLine("Quitting...");
						server.Dispose();
					}
                    
                    break;
				}
				else if (read == "h" || read == "help")
					PrintHelp();
				else if (read.StartsWith("elo"))
				{
					int index = read.IndexOf("=");
					string val = read.Substring(index + 1, read.Length - (index + 1));
					if (int.TryParse(val.Replace(" ", string.Empty), out index))
					{
						Console.WriteLine(string.Format("Elo range set to {0}", index));
						if (index == 0)
							Console.WriteLine("Elo turned off");
						server.EloRange = index;
					}
					else
						Console.WriteLine("Invalid elo range provided (Must be an integer)\n");
				}
				else
					Console.WriteLine("Command not recognized, please try again");
			}
		}

		private static void PrintHelp()
		{
			Console.WriteLine(@"Commands Available
(s)top - Stops hosting
(r)estart - Restarts the hosting service even when stopped
(l)og - Toggles logging (starts enabled)
(q)uit - Quits the application
(h)elp - Get a full list of commands");
		}

	    /// <summary>
	    /// Return the Local IP-Address
	    /// </summary>
	    /// <returns></returns>
	    private static string GetLocalIPAddress()
	    {
	        var host = Dns.GetHostEntry(Dns.GetHostName());
	        foreach (var ip in host.AddressList)
	        {
	            if (ip.AddressFamily == AddressFamily.InterNetwork)
	            {
	                return ip.ToString();
	            }
	        }
	        throw new Exception("No network adapters with an IPv4 address in the system!");
	    }
    }
}