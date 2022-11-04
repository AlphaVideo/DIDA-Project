using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using BankServer.BankDomain;
using Bank;

namespace BankServer.Services
{
	internal class BankServiceImpl : BankService.BankServiceBase
	{
		private PrimaryBackup _datacentre;

		public BankServiceImpl(PrimaryBackup datacentre)
		{ 
			_datacentre = datacentre;
		}


		public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
		{
			var reply = new DepositReply();

			Console.WriteLine("[Bank  ] Received deposit(amount={0})", request.Amount);
			Operation op = new(Operation.OpCode.DEPOSIT, request.Amount, request.CustomerId, request.MsgId);
			_datacentre.queueOperation(op);

			Console.WriteLine("[Bank  ] Will now block awaiting result");
			reply.Balance = op.waitForResult();
            Console.WriteLine("[Bank  ] Result 'Balance={0}' arrived, resuming", reply.Balance);
            reply.ServerType = _datacentre.isPrimaryServer() ? "Primary" : "Secondary";

            return Task.FromResult(reply);
		}

		public override Task<WithdrawalReply> Withdrawal(WithdrawalRequest request, ServerCallContext context)
		{
			var reply = new WithdrawalReply();

			Console.WriteLine("[Bank  ] Received withdrawal(amount={0})", request.Amount);
			Operation op = new(Operation.OpCode.WITHDRAWAL, request.Amount, request.CustomerId, request.MsgId);
			_datacentre.queueOperation(op);

			Console.WriteLine("[Bank  ] Will now block awaiting result");
			reply.Balance = op.waitForResult();
			Console.WriteLine("[Bank  ] Result 'Balance={0}' arrived, resuming", reply.Balance);
            reply.ServerType = _datacentre.isPrimaryServer() ? "Primary" : "Secondary";

            return Task.FromResult(reply);
		}

		public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
		{
			var reply = new ReadBalanceReply();

			Console.WriteLine("[Bank  ] Received readBalance()");
			Operation op = new(Operation.OpCode.READ, 0, request.CustomerId, request.MsgId);
			_datacentre.queueOperation(op);

			Console.WriteLine("[Bank  ] Will now block awaiting result");
			reply.Balance = op.waitForResult();
            Console.WriteLine("[Bank  ] Result 'Balance={0}' arrived, resuming", reply.Balance);
			reply.ServerType = _datacentre.isPrimaryServer() ? "Primary" : "Secondary";

            return Task.FromResult(reply);
		}

	}
}
