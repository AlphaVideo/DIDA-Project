/* using DADBankClientClient; */
using Common;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

internal class Customer
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        List<ServerInfo> bankServers = new();
        bankServers.Add(new ServerInfo("localhost", 1001));

        var request = new ReadBalanceRequest();
        foreach (ServerInfo server in bankServers)
        {
            var client = new BankService.BankServiceClient(server.GetChannel());
            var reply = client.ReadBalance(request);
            Console.WriteLine("reply: " + reply.Status);
        }

        //var clientInterceptor = new ClientInterceptor();
        //GrpcChannel channel = GrpcChannel.ForAddress("http://" + ServerHostname + ":" + ServerPort);
        //CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
        //var client = new BankService.BankServiceClient(interceptingInvoker);

        Console.ReadKey();
    }

    public class ClientInterceptor : Interceptor
    {
        // private readonly ILogger logger;

        //public GlobalServerLoggerInterceptor(ILogger logger) {
        //    this.logger = logger;
        //}

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {

            Metadata metadata = context.Options.Headers; // read original headers
            metadata ??= new Metadata();
            metadata.Add("dad", "dad-value"); // add the additional metadata

            // create new context because original context is readonly
            ClientInterceptorContext<TRequest, TResponse> modifiedContext =
                new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host,
                    new CallOptions(metadata, context.Options.Deadline,
                        context.Options.CancellationToken, context.Options.WriteOptions,
                        context.Options.PropagationToken, context.Options.Credentials));
            Console.Write("calling server...");
            TResponse response = base.BlockingUnaryCall(request, modifiedContext, continuation);
            return response;
        }

    }
}