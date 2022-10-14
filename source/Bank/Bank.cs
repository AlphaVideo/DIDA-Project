using BankServer.BankDomain;
using BankServer.Services;
using Common;
using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/* GRPC class methods */
/*TODO: Decide if we're going to use interceptors
 * Also clean up code and adapt it for our project */

internal class BankApp
{
    private static int processId;
    private static int serverPort;
    private static Timeslots? timeslots;
    private static List<BoneyServerInfo> boneyServers = new();
    private static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Error: unexpected number of arguments, expected 1, got " + args.Length + " instead.");
            Console.ReadKey();
            System.Environment.Exit(1);
        }

        processId = int.Parse(args[0]);

        Console.SetWindowSize(70, 20);
        Console.WriteLine("BANK process started with id " + processId);

        readConfig();

        const string serverHostname = "localhost";
        BankStore store = new();
        BankServiceImpl service = new BankServiceImpl(store);

        Server server = new Server
        {
            Services = { BankService.BindService(service).Intercept(new ServerInterceptor()) },
            Ports = { new ServerPort(serverHostname, serverPort, ServerCredentials.Insecure) }
        };
        server.Start();

        Console.WriteLine("Bank server listening on port " + serverPort);
        Console.WriteLine("Press enter to exit.");

        //Timeslots start with index 1!!
        for(int i = 1; i <= 3; i++)
        {
            //Assuming bank servers are running as processes 4,5 and 6
            //Asks for consensus on who's the leader with random bank server as invalue candidate
            Random rnd = new Random();
            int candidate = rnd.Next(4, 7);

            foreach (ServerInfo boney in boneyServers)
            {
                Thread thread = new Thread(() => requestConsensus(boney, i, candidate));
                thread.Start();
            }
            Thread.Sleep(1000); //TODO tirar isto, é só para testar, usar ManualResetEvent
        }

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();

        Console.WriteLine("Server will now shutdown.");
        server.ShutdownAsync().Wait();
    }

    private static void requestConsensus(ServerInfo boney, int slot, int value)
    {
        try
        {
            var request = new CompareSwapRequest { Slot = slot, Invalue = value };
            var boneyClient = new BoneyService.BoneyServiceClient(boney.Channel);
            var reply = boneyClient.CompareAndSwap(request);
            Console.WriteLine("[{0}] Consensus: server {1} is primary for slot {2}.",
                boney.Address, reply.Outvalue, slot); // TODO in real system get real timestamp instead of `i`
        }
        catch (Grpc.Core.RpcException) // Server down (different from frozen)
        {
            Console.WriteLine("Boney server " + boney + " could not be reached.");
        }
    }

    private static void readConfig()
    {
        string base_path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\"));
        string config_path = Path.Combine(base_path, @"Common\config.txt");

        int slot_duration = 0, slot_count = 0;

        string[] lines = File.ReadAllLines(config_path);
        foreach (string line in lines)
        {
            string[] tokens = line.Split(" ");
            if (tokens.Length == 4 && tokens[0] == "P" && tokens[2] == "boney")
                boneyServers.Add(new BoneyServerInfo(tokens[3]));

            if (tokens.Length == 4 && tokens[0] == "P" && tokens[1] == processId.ToString())
                serverPort = int.Parse(tokens[3].Split(":")[2]);

            if (tokens.Length == 2 && tokens[0] == "S")
                slot_count = int.Parse(tokens[1]);

            if (tokens.Length == 2 && tokens[0] == "D")
                slot_duration = int.Parse(tokens[1]);
        }
        timeslots = new Timeslots(slot_duration, slot_count);
    }
}

public class ServerInterceptor : Interceptor
{

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        string callId = context.RequestHeaders.GetValue("dad");
        Console.WriteLine("DAD header: " + callId);
        return base.UnaryServerHandler(request, context, continuation);
    }

}