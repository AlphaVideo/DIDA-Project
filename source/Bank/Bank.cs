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
    private static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Error: unexpected number of argumentos, expected 1, got " + args.Length + " instead.");
            Console.ReadKey();
            System.Environment.Exit(1);
        }

        int processId = int.Parse(args[0]);
        int ServerPort = 0;

        Console.WriteLine("Bank process started with id " + processId);

        // TODO config parse and setup

        try
        {
            Console.WriteLine("Input port number:");
            var portString = Console.ReadLine();
            ServerPort = Int32.Parse(portString);
        } catch (System.FormatException)
        {
            Console.WriteLine("Port must be a valid integer.");
            Environment.Exit(-1);
        }

        //Hardcoded timeslots, duration 50 ms for 10 slots
        Timeslots timeslots = new Timeslots(50, 10);
        
        const string ServerHostname = "localhost";
        BankStore store = new();
        BankServiceImpl service = new BankServiceImpl(store);

        Server server = new Server
        {
            Services = { BankService.BindService(service).Intercept(new ServerInterceptor()) },
            Ports = { new ServerPort(ServerHostname, ServerPort, ServerCredentials.Insecure) }
        };
        server.Start();

        Console.WriteLine("Bank server listening on port " + ServerPort);
        Console.WriteLine("Press T to toggle freeze state. Press enter to exit.");
        var key = Console.ReadKey().Key;

        while (key != ConsoleKey.Enter)
        {
            if (key == ConsoleKey.T)
                Console.WriteLine("Running state set to " + service.ToggleIsRunning());
            key = Console.ReadKey().Key;
        }

        Console.WriteLine("Server will now shutdown.");
        server.ShutdownAsync().Wait();
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