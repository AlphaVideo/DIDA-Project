using Grpc.Core.Interceptors;
using Grpc.Core;


class TestServer
{
    static void Main(string[] args)
    {
        const int ServerPort = 1001;
        const string ServerHostname = "localhost";

        Server server = new Server
        {
            Services = { TestService.BindService(new TestServiceImpl()).Intercept(new ServerInterceptor()) },
            Ports = { new ServerPort(ServerHostname, ServerPort, ServerCredentials.Insecure) }
        };
        server.Start();
        Console.WriteLine("ChatServer server listening on port " + ServerPort);
        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();

    }
}
internal class TestServiceImpl : TestService.TestServiceBase
{
    public TestServiceImpl()
    {
    }

    public override Task<Reply> Test(Request request, ServerCallContext context)
    {
        Console.WriteLine("Tests request being served");
        return Task.FromResult(new Reply());
    }
}

public class ServerInterceptor : Interceptor
{

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        bool cancel = false;

        Console.WriteLine("Intercepted incoming call");

        if (cancel)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "Server is Frozen"));
        }

        Task<TResponse> reply = base.UnaryServerHandler(request, context, continuation);
        Console.WriteLine("Intercepted outgoing reply");
        return reply;
    }

}