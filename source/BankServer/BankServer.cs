using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/* GRPC class methods */
/*TODO: Decide if we're going to use interceptors
 * Also clean up code and adapt it for our project */

public class DADBankClientService : BankClientService.BankClientServiceBase
{
    private Dictionary<string, string> clientMap = new Dictionary<string, string>();

    public DADBankClientService() {}

    public override Task<BankClientReply> Test(
       BankClientRequest request, ServerCallContext context)
    {
        return Task.FromResult(Reg(request));
    }

    public BankClientReply Reg(BankClientRequest request)
    {

        lock (this)
        {
            Console.WriteLine($"Received request with message: {request.Message}");
        }
        return new BankClientReply
        {
            Status = true
        };
    }
}

internal class BankServer
{
    private static void Main(string[] args)
    {
        const int ServerPort = 1001;
        const string ServerHostname = "localhost";

        Server server = new Server
        {
            Services = { BankClientService.BindService(new DADBankClientService()).Intercept(new ServerInterceptor()) },
            Ports = { new ServerPort(ServerHostname, ServerPort, ServerCredentials.Insecure) }
        };
        server.Start();
        Console.WriteLine("ChatServer server listening on port " + ServerPort);
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