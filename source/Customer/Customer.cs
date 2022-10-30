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
		int customerId;
		int msgId = 0;
		List<BankServerInfo> bankServers = new();
		Config config = new();

		AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

		Console.SetWindowSize(60, 20);

		if (argv.Length != 2)
		{
			Console.WriteLine("Error: unexpected number of arguments, expected 2, got " + argv.Length + " instead.");
			Console.ReadKey();
			System.Environment.Exit(1);
		}

		customerId = int.Parse(argv[0]);

		string inputMode = argv[1]; //"script" or "cmd"
		string base_path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\"));
        string script_path = Path.Combine(base_path, @"Customer\customer_script.txt");
		StreamReader sr = new StreamReader(script_path);

		Console.WriteLine("CUSTOMER process started with id " + customerId);

		foreach (string addr in config.getBankServerAddresses())
		{
			bankServers.Add(new BankServerInfo(addr));
		}

		string ?command = null;
		bool running = true;
		while(running)
		{
			if(inputMode == "cmd")
			{
				Console.Write("> ");
				command = Console.ReadLine();
			}
			else if (inputMode == "script")
			{
				command = sr.ReadLine();
			}

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
						request.CustomerId = customerId;
						request.MsgId = msgId++;

						if (tokens.Length < 2)
						{
							Console.WriteLine("Deposit command expects 1 additional argument.");
							break;
						}
						//else if (!(int.TryParse(tokens[1], out int numI) || float.TryParse(tokens[1], out float numL)))
						//{
						//	Console.WriteLine("Deposit command expects int or float argument.");
						//	break;
						//}

                        foreach (BankServerInfo server in bankServers)
						{
							Thread thread = new Thread(() => doDeposit(server, request));
							thread.Start();        
						}
						break;
					}

					//W amount - withdrawal
					case "w":
					case "W":
					{
						var request = new WithdrawalRequest();
						request.CustomerId = customerId;
						request.MsgId = msgId++;


                        if (tokens.Length < 2)
						{
							Console.WriteLine("Withdrawal command expects 1 additional argument.");
							break;
						}
						//else if (!(int.TryParse(tokens[1], out int numI) || float.TryParse(tokens[1], out float numF)))
						//{
						//	Console.WriteLine("Withdrawal command expects int or float argument.");
						//	break;
						//}

						foreach (BankServerInfo server in bankServers)
						{
							Thread thread = new Thread(() => doWidthrawal(server, request));
							thread.Start();
						}
						break;
					}

					//R - read
					case "r":
					case "R":
					{
						var request = new ReadBalanceRequest();
						request.CustomerId = customerId;
						request.MsgId = msgId++;

						foreach (BankServerInfo server in bankServers)
						{
							Thread thread = new Thread(() => doReadBalance(server, request));
							thread.Start();
						}
						break;
					}

                    //S - wait/stop
                    case "s":
                    case "S":
					{
                        if (tokens.Length < 2)
                        {
                            Console.WriteLine("Wait command expects 1 additional argument.");
                            break;
                        }
                        else if (!(int.TryParse(tokens[1], out int num)))
                        {
                            Console.WriteLine("Wait command expects int argument.");
                            break;
                        }

						Thread.Sleep(int.Parse(tokens[1]));
                        break;
					}

                    //E - exit
                    case "e":
					case "E":
						running = false;
						break;

					default:
						Console.WriteLine("Could not recognize command: {0}\n(exit with E)", command);
						break;
				}
			}
		}

		//var clientInterceptor = new ClientInterceptor();
		//GrpcChannel channel = GrpcChannel.ForAddress("http://" + ServerHostname + ":" + ServerPort);
		//CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
		//var client = new BankService.BankServiceClient(interceptingInvoker);
	}

	private static void doDeposit(BankServerInfo server, DepositRequest req)
	{
		try
		{
			var reply = server.Client.Deposit(req);
			Console.WriteLine("[{0}] balance={1}", server.Address, reply.Balance);
		}
		catch (Grpc.Core.RpcException) // Server down (different from frozen)
		{
			Console.WriteLine("Server " + server + " could not be reached.");
		}
	}

	private static void doWidthrawal(BankServerInfo server, WithdrawalRequest req)
	{
		try
		{
			var reply = server.Client.Withdrawal(req);
			Console.WriteLine("[{0}] balance={1}", server.Address, reply.Balance);
		}
		catch (Grpc.Core.RpcException) // Server down (different from frozen)
		{
			Console.WriteLine("Server " + server + " could not be reached.");
		}
	}

	private static void doReadBalance(BankServerInfo server, ReadBalanceRequest req)
	{
		try
		{
			var reply = server.Client.ReadBalance(req);
			Console.WriteLine("[{0}] balance={1}", server.Address, reply.Balance);
		}
		catch (Grpc.Core.RpcException) // Server down (different from frozen)
		{
			Console.WriteLine("Server " + server + " could not be reached.");
		}
	}

	//public class ClientInterceptor : Interceptor
	//{
	//    // private readonly ILogger logger;

	//    //public GlobalServerLoggerInterceptor(ILogger logger) {
	//    //    this.logger = logger;
	//    //}

	//    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
	//    {

	//        Metadata metadata = context.Options.Headers; // read original headers
	//        metadata ??= new Metadata();
	//        metadata.Add("dad", "dad-value"); // add the additional metadata

	//        // create new context because original context is readonly
	//        ClientInterceptorContext<TRequest, TResponse> modifiedContext =
	//            new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host,
	//                new CallOptions(metadata, context.Options.Deadline,
	//                    context.Options.CancellationToken, context.Options.WriteOptions,
	//                    context.Options.PropagationToken, context.Options.Credentials));
	//        Console.Write("calling server...");
	//        TResponse response = base.BlockingUnaryCall(request, modifiedContext, continuation);
	//        return response;
	//    }

	//}
}