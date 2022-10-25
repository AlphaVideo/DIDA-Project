using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;
using static Grpc.Core.Interceptors.Interceptor;

internal class Client
{

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        const int ServerPort = 1001;
        const string ServerHostname = "localhost";
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var clientInterceptor = new ClientInterceptor();
        GrpcChannel channel = GrpcChannel.ForAddress("http://" + ServerHostname + ":" + ServerPort);
        CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);

        var client = new TestService.TestServiceClient(interceptingInvoker);

        Request request = new Request { MyInt = 5 };

        try
        {
            Reply reply = client.Test(request);
            Console.WriteLine("Reply received!");

        } catch (RpcException e)
        {
            Console.WriteLine(e.Message);
        }


        Console.ReadKey();
    }
}
public class ClientInterceptor : Interceptor {

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {

        bool cancel = false;

        //ClientInterceptorContext<TRequest, TResponse> modifiedContext =
        //    new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, context.Options.WithCancellationToken(new System.Threading.CancellationToken(cancel)));
        if (cancel)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "I am frozen!"));
        }
        Console.Write("Intercepted outgoing call...\n");
        TResponse response = base.BlockingUnaryCall(request, context, continuation);

        //TResponse response = base.BlockingUnaryCall(request, modifiedContext, continuation);
        Console.WriteLine("Intercepted incomming response...\n");
        return response;
    }
}