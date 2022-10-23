﻿using Boney;
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

        Console.SetWindowSize(60, 20);
        Console.WriteLine("BONEY process started with id " + processId);

        List<ServerInfo> boneyServers = new();
        int serverPort = readConfig(processId, boneyServers);

        Paxos paxos = new Paxos(processId, boneyServers);

        const string ServerHostname = "localhost";
        BoneyServiceImpl boneyService = new BoneyServiceImpl(paxos);
        PaxosServiceImpl paxosService = new PaxosServiceImpl(paxos);

        Server server = new Server
        {
            Services = { BoneyService.BindService(boneyService), PaxosService.BindService(paxosService) },
            Ports = { new ServerPort(ServerHostname, serverPort, ServerCredentials.Insecure) }
        };

        Console.WriteLine("Boney server will begin handling requests at " + startupTime.ToString("h:mm:ss tt"));
        while (DateTime.Now < startupTime) { /* do nothing */ }

        server.Start();
        Console.WriteLine("Boney server listening on port " + serverPort);

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
        Console.WriteLine("Boney server will now shutdown.");
        server.ShutdownAsync().Wait();
    }

    private static int readConfig(int processId, List<ServerInfo> boneyServers)
    {
        string base_path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\"));
        string config_path = Path.Combine(base_path, @"Common\config.txt");

        int serverPort = 0;

        string[] lines = File.ReadAllLines(config_path);
        foreach (string line in lines)
        {
            string[] tokens = line.Split(" ");
            if (tokens.Length == 4 && tokens[0] == "P" && tokens[2] == "boney")
                boneyServers.Add(new ServerInfo(tokens[3]));

            if (tokens.Length == 4 && tokens[0] == "P" && tokens[1] == processId.ToString())
                serverPort = int.Parse(tokens[3].Split(":")[2]);

        }
        return serverPort;
    }
}