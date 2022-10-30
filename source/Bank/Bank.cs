using Bank;
using Bank.Services;
using BankServer.BankDomain;
using BankServer.Services;
using Common;
using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/* GRPC class methods */
/*TODO: Decide if we're going to use interceptors
 * Also clean up code and adapt it for our project */

internal class BankApp
{
    private static int processId;
    private static DateTime startupTime;
    private static int serverPort;
    private static Timeslots? timeslots;
    private static List<ServerInfo> boneyServers = new();
    private static Config config = new();
    
    private static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Error: unexpected number of arguments, expected 2, got " + args.Length + " instead.");
            Console.ReadKey();
            System.Environment.Exit(1);
        }

        processId = int.Parse(args[0]);
        startupTime = DateTime.Parse(args[1]);

        Console.SetWindowSize(60, 20);
        Console.WriteLine("BANK process started with id " + processId);

        serverPort = config.getMyPort(processId);
        timeslots = config.getTimeslots();

        foreach (string addr in config.getBoneyServerAddresses())
        {
            boneyServers.Add(new BoneyServerInfo(addr));
        }

        const string serverHostname = "localhost";
        BankStore store = new();
        PrimaryBackup primaryBackup = new(store, new PerfectChannel(config.getSlotDuration()));

        BankServiceImpl bankService = new BankServiceImpl(primaryBackup);
        PrimaryBackupServiceImpl backupService = new(primaryBackup);

        Server server = new Server
        {
            Services = { BankService.BindService(bankService), PrimaryBackupService.BindService(backupService) },
            Ports = { new ServerPort(serverHostname, serverPort, ServerCredentials.Insecure) }
        };
        Console.WriteLine("Bank server will begin handling requests at " + startupTime.ToString("HH:mm:ss"));
        while (DateTime.Now < startupTime) { /* do nothing */ }

        server.Start();
        Console.WriteLine("Bank server listening on port " + serverPort);


        for (int slotId = 1;  slotId <= timeslots.getMaxSlots(); slotId++) 
        {
            //TODO - Check if there's a prettier way to do a Frozen check
            //bankService.setIsRunning(!timeslots.isFrozen(slotId, processId));
            
            //1st time slot starts right away => !timer.IsRunning condition
            if (!timeslots.isFrozen(slotId, processId))
            {
                Random rnd = new Random();
                int candidate = rnd.Next(4, 7);

                foreach (ServerInfo boney in boneyServers)
                {
                    Thread thread = new Thread(() => requestConsensus(boney, slotId, candidate));
                    thread.Start();
                }
            }
            Thread.Sleep(timeslots.getSlotDuration());
        }

        Console.WriteLine("Reached end of simulation. Press any key to exit");
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
            Console.WriteLine("[{0}] Server {1} is primary for slot {2}.",
                boney.Address, reply.Outvalue, slot);
        }
        catch (Grpc.Core.RpcException) // Server down (different from frozen)
        {
            Console.WriteLine("Boney server " + boney + " could not be reached.");
        }
    }
}

//public class ServerInterceptor : Interceptor
//{
//    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
//    {
//        string callId = context.RequestHeaders.GetValue("dad");
//        Console.WriteLine("DAD header: " + callId);
//        return base.UnaryServerHandler(request, context, continuation);
//    }

//}