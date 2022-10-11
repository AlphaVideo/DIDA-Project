/* using DADBankClientClient; */
using Common;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

internal class Customer
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] argv)
    {
        int clientId;
        List<BankServerInfo> bankServers;

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        if (argv.Length != 1)
        {
            Console.WriteLine("Error: unexpected number of argumentos, expected 1, got " + argv.Length + " instead.");
            Console.ReadKey();
            System.Environment.Exit(1);
        }

        clientId = int.Parse(argv[0]);
        Console.WriteLine("CUSTOMER process started with id " + clientId);

        bankServers = readConfig();      


        bool running = true;
        while(running)
        {
            Console.Write("> ");
            var command = Console.ReadLine();
            if(command != null)
            {
                string[] tokens = command.Split(' ');

                switch (tokens[0])
                {
                    //D amount - deposit
                    case "d":
                    case "D":
                    {
                        var request = new DepositRequest();

                        foreach (BankServerInfo server in bankServers)
                        {
                            try
                            {
                                var reply = server.Client.Deposit(request);
                                Console.WriteLine("reply from " + server.Address + " -> " + reply.Status);
                            }
                            catch (Grpc.Core.RpcException) // Server down (different from frozen)
                            {
                                Console.WriteLine("Server " + server + " could not be reached.");
                            }
                        }
                        break;
                    }

                    //W amount - withdrawal
                    case "w":
                    case "W":
                    {
                        var request = new WithdrawalRequest();

                        foreach (BankServerInfo server in bankServers)
                        {
                            try
                            {
                                var reply = server.Client.Withdrawal(request);
                                Console.WriteLine("reply from " + server.Address + " -> " + reply.Status);
                            }
                            catch (Grpc.Core.RpcException) // Server down (different from frozen)
                            {
                                Console.WriteLine("Server " + server + " could not be reached.");
                            }
                        }
                        break;
                    }

                    //R - read
                    case "r":
                    case "R":
                    {
                        var request = new ReadBalanceRequest();

                        foreach (BankServerInfo server in bankServers)
                        {
                            try
                            {
                                var reply = server.Client.ReadBalance(request);
                                Console.WriteLine("reply from " + server.Address + " -> " + reply.Status);
                            }
                            catch (Grpc.Core.RpcException) // Server down (different from frozen)
                            {
                                Console.WriteLine("Server " + server + " could not be reached.");
                            }
                        }
                        break;
                    }

                    //E - exit
                    case "e":
                    case "E":
                        running = false;
                        Console.WriteLine("App is now closing. Press enter to continue.");
                        break;

                    default:
                        Console.WriteLine("Could not recognize command. (exit with E)");
                        break;
                }
            }
        }

        //var clientInterceptor = new ClientInterceptor();
        //GrpcChannel channel = GrpcChannel.ForAddress("http://" + ServerHostname + ":" + ServerPort);
        //CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
        //var client = new BankService.BankServiceClient(interceptingInvoker);

        Console.ReadKey();
    }

    public static List<BankServerInfo> readConfig()
    {
        string base_path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\"));
        string config_path = Path.Combine(base_path, @"Common\config.txt");

        List<BankServerInfo> servers = new();

        string[] lines = File.ReadAllLines(config_path);
        foreach (string line in lines)
        {
            string[] tokens = line.Split(" ");
            if (tokens.Length == 4 && tokens[0] == "P" && tokens[2] == "bank")
                servers.Add(new BankServerInfo(tokens[3]));
        }
        return servers;
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