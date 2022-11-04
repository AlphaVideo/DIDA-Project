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
using System.Threading;
using System.Threading.Tasks;


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

		serverPort = config.getPortFromPid(processId);
		timeslots = config.getTimeslots();

		foreach (string addr in config.getBoneyServerAddresses())
		{
			boneyServers.Add(new BoneyServerInfo(addr));
		}


		PerfectChannel perfectChannel = new PerfectChannel(config.getTimeslots().getSlotDuration());
		Freezer freezer = new Freezer(processId, perfectChannel, config.getTimeslots());
		freezer.StartAt(startupTime);


		const string serverHostname = "localhost";
		BankStore store = new();
		PrimaryBackup primaryBackup = new(store, serverPort, processId, perfectChannel, startupTime);

		BankServiceImpl bankService = new BankServiceImpl(primaryBackup);
		PrimaryBackupServiceImpl backupService = new(primaryBackup);

		Server server = new Server
		{
			Services = {
				BankService.BindService(bankService).Intercept(perfectChannel),
				PrimaryBackupService.BindService(backupService).Intercept(perfectChannel) },
			Ports = { new ServerPort(serverHostname, serverPort, ServerCredentials.Insecure) }
		};
		Console.WriteLine("Bank server will begin handling requests at " + startupTime.ToString("HH:mm:ss"));

		server.Start();
		Console.WriteLine("Bank server listening on port " + serverPort);

		Console.WriteLine("Press any key to exit");
		Console.ReadKey();

		Console.WriteLine("Server will now shutdown.");
		server.ShutdownAsync().Wait();
	}
}