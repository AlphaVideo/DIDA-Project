using BankServer.BankDomain;
using BankServer.Services;
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
        const int ServerPort = 1001;
        const string ServerHostname = "localhost";
        BankStore store = new BankStore();

        Server server = new Server
        {
            Services = { BankService.BindService(new BankServiceImpl(store)).Intercept(new ServerInterceptor()) },
            Ports = { new ServerPort(ServerHostname, ServerPort, ServerCredentials.Insecure) }
        };
        server.Start();

        Console.WriteLine("Bank server listening on port " + ServerPort);
        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

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