using BankServer.BankDomain;
using Common;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Bank
{
	internal class PrimaryBackup
	{
		private List<Operation> _uncommited = new();
		private SortedList<int, Operation> _commited = new();
		private List<Operation> _executed = new();
		private int _lastExecuted = 0;

		private int _processId;
		private int _currentPrimary;
		private Config _config;

		private BankStore _store;
		private PrimaryBackupService.PrimaryBackupServiceClient[] _banks;

		internal PrimaryBackup(BankStore store, int processId, List<string> bankAddrs, PerfectChannel perfectChannel)
		{
			_store = store;
			_processId = processId;
			_banks = new PrimaryBackupService.PrimaryBackupServiceClient[bankAddrs.Count];

			_config = new();

			int i = 0;
			foreach (string addr in bankAddrs)
			{
				_banks[i++] = new PrimaryBackupService.PrimaryBackupServiceClient(GrpcChannel.ForAddress(addr).Intercept(perfectChannel));
			}
		}

		internal void queueOperation(Operation op)
		{
			_uncommited.Add(op);

			if (_processId == _currentPrimary)
			{
				Thread committer = new Thread(() => Do2PhaseCommit(op));
				committer.Start();
			}
		}

		internal void Do2PhaseCommit(Operation op)
		{
			PrepareRequest pReq = new();
			pReq.CustomerId = op.CustomerId;
			pReq.MsgId = op.MessageId;
			pReq.SeqNumber = generateSeqNumber();


			while (!broadcastPrepare(pReq))
			{
				if (_processId != _currentPrimary) return;

				int newSeqNum = generateSeqNumber(); // just to see if something has changed

				if (newSeqNum == pReq.SeqNumber) return;

				pReq.SeqNumber = newSeqNum;
			}


			CommitRequest cReq = new();
			cReq.CustomerId = op.CustomerId;
			cReq.MsgId = op.MessageId;
			cReq.SeqNumber = pReq.SeqNumber;

			Console.WriteLine("[PrmBck] Broadcasting commit(custmrId={0}, msgId={1}, seq={2})", cReq.CustomerId, cReq.CustomerId, cReq.SeqNumber);
			foreach (PrimaryBackupService.PrimaryBackupServiceClient client in _banks)
			{
				Thread thread = new Thread(() => client.Commit(cReq));
				thread.Start();
			}

		}

		internal int generateSeqNumber()
		{
			int last = _lastExecuted;
			foreach (int key in _commited.Keys)
			{
				if (key != last + 1)
				{
					return last + 1;
				}
				last = key;
			}
			return last + 1;
		}

		internal bool broadcastPrepare(PrepareRequest req)
		{
			Task<bool>[] pendingRequests = new Task<bool>[_banks.Length];
			List<Task<bool>> completedRequests = new List<Task<bool>>();

			Console.WriteLine("[PrmBck] Broadcasting prepare(custmrId={0}, msgId={1}, seq={2})", req.CustomerId, req.CustomerId, req.SeqNumber);

			for (int i = 0; i < _banks.Length; i++)
			{
				PrimaryBackupService.PrimaryBackupServiceClient client = _banks[i];

				pendingRequests[i] = Task.Factory.StartNew<bool>(() => sendPrepare(client, req));
			}

			// Wait for a majority of answers
			while (pendingRequests.Length > completedRequests.Count)
			{
				int completedIndex = Task.WaitAny(pendingRequests);
				completedRequests.Add(pendingRequests[completedIndex]);

				for (int i = 0; i < pendingRequests.Length - 1; i++)
				{
					if (i >= completedIndex) { pendingRequests[i] = pendingRequests[i + 1]; }
				}

				Array.Resize(ref pendingRequests, pendingRequests.Length - 1);
			}

			// if one server answered with NACK, abort
			foreach (Task<bool> task in completedRequests)
			{
				if (!task.Result) return false;
			}
			return true;
		}

		internal bool sendPrepare(PrimaryBackupService.PrimaryBackupServiceClient client, PrepareRequest req)
		{
			try
			{
				var reply = client.Prepare(req);
				return reply.Ack;
			}
			catch (RpcException) // Server down (different from frozen)
			{
				Console.WriteLine("Server " + client.ToString() + " could not be reached.");
				return false;
			}
		}

		internal void commitOperation(int customerId, int msgId, int seqNum)
		{
			Operation? op = _uncommited.Find(el => el.CustomerId == customerId && el.MessageId == msgId);

			if (op == null)
				return; // either already commited or unknown

			op.SeqNum = seqNum;
			_uncommited.Remove(op);
			_commited.Add(seqNum, op); // move to commited list, with priority=seqNum

			executeAllPossible();
		}

		internal void executeAllPossible()
		{
			if (_commited.Count == 0)
				return;

			int headSeqNum = _commited.Keys[0];
			Operation headOp = _commited[headSeqNum];

			while (headOp.SeqNum == _lastExecuted + 1)
			{
				headOp.executeOn(_store);
				_lastExecuted = headOp.SeqNum;

				_commited.Remove(headSeqNum);
				_executed.Add(headOp);

				if (_commited.Count == 0)
					return;

				headSeqNum = _commited.Keys[0];
				headOp = _commited[headSeqNum];
			}
		}

		internal bool canPrepare(string url, int seq)
		{
			int port = int.Parse(url.Split(":")[2]);
			int pid = _config.getPidFromPort(port);

			// i) requester must be primary ii) seq must not be in commited list iii) seq must not have been executed
			return pid == _currentPrimary && !_commited.ContainsKey(seq) && _lastExecuted < seq;
		}
	}
}
