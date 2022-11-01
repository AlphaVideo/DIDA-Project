using Boney;
using Common;
using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Error: unexpected number of arguments, expected 2, got " + args.Length + " instead.");
            Console.ReadKey();
            System.Environment.Exit(1);
        }

        int processId = int.Parse(args[0]);
        DateTime startupTime = DateTime.Parse(args[1]);

        Config config = new();

        Console.SetWindowSize(60, 20);
        Console.WriteLine("BONEY process started with id " + processId);

        List<ServerInfo> boneyServers = new();
        foreach (string addr in config.getBankServerAddresses())
        {
            boneyServers.Add(new BoneyServerInfo(addr));
        }
        int serverPort = config.getMyPort(processId);

        Console.WriteLine("Boney server will begin handling requests at " + startupTime.ToString("HH:mm:ss"));
        while (DateTime.Now < startupTime) { /* do nothing */ }
        Console.WriteLine("Started now");

        PerfectChannel perfectChannel = new PerfectChannel(config.getTimeslots().getSlotDuration());
        Freezer freezer = new Freezer(processId, perfectChannel, config.getTimeslots());
        freezer.StartAt(startupTime);

        Paxos paxos = new Paxos(processId, boneyServers, perfectChannel);

        const string ServerHostname = "localhost";
        BoneyServiceImpl boneyService = new BoneyServiceImpl(paxos);
        PaxosServiceImpl paxosService = new PaxosServiceImpl(paxos);

        Server server = new Server
        {
            Services = { 
                BoneyService.BindService(boneyService).Intercept(perfectChannel), 
                PaxosService.BindService(paxosService).Intercept(perfectChannel)
            },
            Ports = { new ServerPort(ServerHostname, serverPort, ServerCredentials.Insecure) }
        };

        server.Start();
        Console.WriteLine("Boney server listening on port " + serverPort);

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
        Console.WriteLine("Boney server will now shutdown.");
        server.ShutdownAsync().Wait();
    }
}