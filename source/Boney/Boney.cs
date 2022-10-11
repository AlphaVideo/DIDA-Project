﻿using Boney;
using Common;
using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Error: unexpected number of arguments, expected 1, got " + args.Length + " instead.");
            Console.ReadKey();
            System.Environment.Exit(1);
        }

        int processId = int.Parse(args[0]);
        int ServerPort = 0;

        Console.WriteLine("BONEY process started with id " + processId);
        

        //TODO - Read from config file
        try
        {
            Console.WriteLine("Input port number:");
            var portString = Console.ReadLine();
            ServerPort = Int32.Parse(portString);
        }
        catch (System.FormatException)
        {
            Console.WriteLine("Port must be a valid integer.");
            Environment.Exit(-1);
        }

        //Hardcoded - add other boney servers (exclude self)
        int[] ports = { 2001, 2002, 2003 };
        List<ServerInfo> boneyServers = new();
        foreach(int port in ports) {
            if(port != ServerPort)
                boneyServers.Add(new ServerInfo("localhost", port));
        }

        const string ServerHostname = "localhost";
        BoneyServiceImpl boneyService = new BoneyServiceImpl();
        PaxosServiceImpl paxosService = new PaxosServiceImpl();

        Server server = new Server
        {
            Services = { BoneyService.BindService(boneyService), PaxosService.BindService(paxosService) },
            Ports = { new ServerPort(ServerHostname, ServerPort, ServerCredentials.Insecure) }
        };
        server.Start();

        //TODO - Create Paxos object here? Or maybe in BoneyImpl

        Console.WriteLine("Boney server listening on port " + ServerPort);
        Console.WriteLine("Press enter to exit.");

        Console.WriteLine("Boney server will now shutdown.");
        server.ShutdownAsync().Wait();
    }
}