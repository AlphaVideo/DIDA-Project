﻿/* using DADBankClientClient; */
using Common;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
		List<BankService.BankServiceClient> bankServers = new();
		Config config = new();

		AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

		Console.SetWindowSize(60, 20);

		if (argv.Length != 4)
		{
			Console.WriteLine("Error: unexpected number of arguments, expected 4, got " + argv.Length + " instead.");
			Console.ReadKey();
			System.Environment.Exit(1);
		}

		customerId = int.Parse(argv[0]);
		string inputMode = argv[1]; //"script" or "cmd"
		DateTime startupTime = DateTime.Parse(argv[2]);

		PerfectChannel perfectChannel = new(config.getTimeslots().getSlotDuration());

		string base_path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\"));
		string script_path = Path.Combine(base_path, @"Customer\" + argv[3]);
		StreamReader sr = new StreamReader(script_path);

		Console.WriteLine("CUSTOMER process started with id " + customerId);

		foreach (string addr in config.getBankServerAddresses())
		{
			bankServers.Add(new BankService.BankServiceClient(GrpcChannel.ForAddress(addr).Intercept(perfectChannel)).WithHost(addr));
		}

		Console.WriteLine("Customer will begin sending requests at " + startupTime.ToString("HH:mm:ss"));
		Thread.Sleep(startupTime - DateTime.Now);


		string? command = null;
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
				if (sr.EndOfStream)
				{
					Console.WriteLine("Reached end of script. Press any key to exit.");
					Console.ReadKey();
					break;
				}

				command = sr.ReadLine();
				Console.WriteLine("> {0}", command);
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

						try { request.Amount = float.Parse(tokens[1], CultureInfo.InvariantCulture); }
						catch (FormatException) { goto default; }

						if (tokens.Length < 2)
						{
							Console.WriteLine("Deposit command expects 1 additional argument.");
							break;
						}

						broadcastDeposit(bankServers, request);
						break;
					}

					//W amount - withdrawal
					case "w":
					case "W":
					{
						var request = new WithdrawalRequest();
						request.CustomerId = customerId;
						request.MsgId = msgId++;

						try { request.Amount = float.Parse(tokens[1], CultureInfo.InvariantCulture); }
						catch (FormatException) { goto default; }

						if (tokens.Length < 2)
						{
							Console.WriteLine("Withdrawal command expects 1 additional argument.");
							break;
						}

						broadcastWithdrawal(bankServers, request);
						break;
					}

					//R - read
					case "r":
					case "R":
					{
						var request = new ReadBalanceRequest();
						request.CustomerId = customerId;
						request.MsgId = msgId++;

						broadcastReadBalance(bankServers, request);
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
	}

	private static float broadcastDeposit(List<BankService.BankServiceClient> bankServers, DepositRequest req)
	{
		List<Task<float>> pendingRequests = new();

		foreach (BankService.BankServiceClient bank in bankServers)
		{
			pendingRequests.Add(Task.Run(() => doDeposit(bank, req)));
		}

		for (int i = 0; i < pendingRequests.Count; i++)
		{
			int completed = Task.WaitAny(pendingRequests.ToArray());
			float res = pendingRequests[completed].Result;

			if (res != -1) return res;

			pendingRequests.RemoveAt(completed);
		}
		throw new InvalidOperationException("No Bank server could be reached. Can't progress any further.");
	}

	private static float doDeposit(BankService.BankServiceClient server, DepositRequest req)
	{
		try
		{
			var reply = server.Deposit(req);
            Console.WriteLine("[DEPOSIT] Reply received " + reply.ServerType + " server: Balance={0}", reply.Balance);

            return reply.Balance;
		}
		catch (RpcException e) // Server down (different from frozen)
		{
			Console.WriteLine(e);
			Console.WriteLine("Server " + server + " could not be reached.");
			return -1;
		}
	}

	private static float broadcastWithdrawal(List<BankService.BankServiceClient> bankServers, WithdrawalRequest req)
	{
		List<Task<float>> pendingRequests = new();

		foreach (BankService.BankServiceClient bank in bankServers)
		{
			pendingRequests.Add(Task.Run(() => doWithdrawal(bank, req)));
		}

		for (int i = 0; i < pendingRequests.Count; i++)
		{
			int completed = Task.WaitAny(pendingRequests.ToArray());
			float res = pendingRequests[completed].Result;

			if (res != -1) return res;

			pendingRequests.RemoveAt(completed);
		}
		throw new InvalidOperationException("No Bank server could be reached. Can't progress any further.");
	}

	private static float doWithdrawal(BankService.BankServiceClient server, WithdrawalRequest req)
	{
		try
		{
			var reply = server.Withdrawal(req);
            Console.WriteLine("[WITHDRAWAL] Reply received " + reply.ServerType + " server: Balance={0}", reply.Balance);

            return reply.Balance;
		}
		catch (RpcException) // Server down (different from frozen)
		{
			Console.WriteLine("Server " + server + " could not be reached.");
			return -1;
		}
	}

	private static float broadcastReadBalance(List<BankService.BankServiceClient> bankServers, ReadBalanceRequest req)
	{
		List<Task<float>> pendingRequests = new();

		foreach (BankService.BankServiceClient bank in bankServers)
		{
			pendingRequests.Add(Task.Run(() => doReadBalance(bank, req)));
		}

		for (int i = 0; i < pendingRequests.Count; i++)
		{
			int completed = Task.WaitAny(pendingRequests.ToArray());
			float res = pendingRequests[completed].Result;

			if (res != -1) return res;

			pendingRequests.RemoveAt(completed);
		}
		throw new InvalidOperationException("No Bank server could be reached. Can't progress any further.");
	}

	private static float doReadBalance(BankService.BankServiceClient server, ReadBalanceRequest req)
	{
		try
		{
			var reply = server.ReadBalance(req);
			Console.WriteLine("[READ] Reply received " + reply.ServerType + " server: Balance={0}", reply.Balance);

			return reply.Balance;
		}
		catch (RpcException) // Server down (different from frozen)
		{
			Console.WriteLine("Server " + server + " could not be reached.");
			return -1;
		}
	}
}