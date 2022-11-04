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

		private int _port;
		private int _processId;
		private int _currentSlot;
		private Dictionary<int, int> _primaryHistory;
		private ReaderWriterLockSlim _slotLock;
		private Config _config;

		private BankStore _store;
		private PrimaryBackupService.PrimaryBackupServiceClient[] _banks;
		private BoneyService.BoneyServiceClient[] _boneys;

		internal PrimaryBackup(BankStore store, int port, int processId, PerfectChannel perfectChannel, DateTime startupTime)
		{
			_config = new();
			List<string> bankAddrs = _config.getBankServerAddresses();
			List<string> boneyAddrs = _config.getBoneyServerAddresses();

			_store = store;
			_port = port;
			_processId = processId;
			_banks = new PrimaryBackupService.PrimaryBackupServiceClient[bankAddrs.Count];
			_boneys = new BoneyService.BoneyServiceClient[boneyAddrs.Count];

			_primaryHistory = new();
			_slotLock = new();


			int i = 0;
			foreach (string addr in bankAddrs)
			{
				_banks[i++] = new PrimaryBackupService.PrimaryBackupServiceClient(GrpcChannel.ForAddress(addr).Intercept(perfectChannel)).WithHost(addr);
			}
			i = 0;
			foreach (string addr in boneyAddrs)
			{
				_boneys[i++] = new BoneyService.BoneyServiceClient(GrpcChannel.ForAddress(addr).Intercept(perfectChannel)).WithHost(addr);
			}

			Thread updater = new Thread(() => primaryUpdater(startupTime));
			updater.Start();
		}

		// at the beggining of each timeslot asks Boney who is the new primary
		internal void primaryUpdater(DateTime startupTime)
		{
			int slotDuration = _config.getSlotDuration();
			int slotCount = _config.getSlotCount();
			int slot;

			Thread.Sleep(startupTime - DateTime.Now);

			for (slot = 1; slot <= slotCount; slot++)
			{
				_slotLock.EnterWriteLock();
				_currentSlot = slot;
				_primaryHistory[slot] = getLeader(slot);

				if (slot > 1 && _primaryHistory[slot] == _processId && _primaryHistory[slot - 1] != _processId)
					doTakeOver();
				_slotLock.ExitWriteLock();

				Thread.Sleep(slotDuration);
			}
			Console.WriteLine("Last timeslot ({0}) has ENDED", slot - 1);
		}

		// sends request to Boney servers to get the new leader
		internal int getLeader(int slot)
		{
			CompareSwapRequest req = new();
			req.Slot = slot;
			req.Invalue = getLeaderSuggestion(slot);

			List<Task<int>> pendingRequests = new();

			Console.WriteLine("[PrmBck] Requesting leader for slot {0}, sugesting {1}", slot, req.Invalue);
			for (int i = 0; i < _boneys.Length; i++)
			{
				BoneyService.BoneyServiceClient client = _boneys[i];

				pendingRequests.Add(Task.Run(() => sendLeaderRequest(client, req)));
			}

			for (int i = 0; i < pendingRequests.Count; i++)
			{
				int completed = Task.WaitAny(pendingRequests.ToArray());
				int res = pendingRequests[completed].Result;

				if (res != -1)
				{
					Console.WriteLine("[PrmBck] Bank {0} is the leader for slot {1}", res, slot);
					return res;
				}

				pendingRequests.RemoveAt(completed);
			}
			throw new InvalidOperationException("No Boney server could be reached. Can't progress any further.");
		}

		// calculate which leader will this process suggest to be the new primary
		internal int getLeaderSuggestion(int slot)
		{
			// se primario anterior nao for suspeito, sugerir esse
			if (slot > 1 && !_config.getTimeslots().isSuspected(slot, _primaryHistory[slot - 1]))
				return _primaryHistory[slot - 1];

			//senao, sugerir o com pid mais baixo que nao seja suspeito
			List<int> candidateLeaders = _config.getBankIds();

			foreach (int sus in _config.getTimeslots().getMySuspectList(slot, _processId))
				candidateLeaders.Remove(sus);

			return candidateLeaders.Min();
		}

		// sends leader request to Boney
		internal int sendLeaderRequest(BoneyService.BoneyServiceClient client, CompareSwapRequest req)
		{
			try
			{
				var reply = client.CompareAndSwap(req);
				return reply.Outvalue;
			}
			catch (RpcException) // Server down (different from frozen)
			{
				Console.WriteLine("Server " + client.ToString() + " could not be reached.");
				return -1;
			}
		}

		// inserts operation in uncommited queue and, if primary, start 2-Phase Commit
		internal void queueOperation(Operation op)
		{
			_uncommited.Add(op);

			_slotLock.EnterReadLock();
			int currentPrimary = _primaryHistory[_currentSlot];
			_slotLock.ExitReadLock();

			if (_processId == currentPrimary)
			{
				Thread committer = new Thread(() => Do2PhaseCommit(op));
				committer.Start();
			}
		}

		// executes 2-Phase Commit protocol with other banks
		internal void Do2PhaseCommit(Operation op)
		{
			PrepareRequest pReq = new();
			pReq.CustomerId = op.CustomerId;
			pReq.MsgId = op.MessageId;
			pReq.SeqNumber = generateSeqNumber();
			pReq.SenderPort = _port;


			while (!broadcastPrepare(pReq))
			{
				_slotLock.EnterReadLock();
				int currentPrimary = _primaryHistory[_currentSlot];
				_slotLock.ExitReadLock();

				if (_processId != currentPrimary) return;

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

		// calculate new sequence number to try and atribute to new operation
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

		// broadcast prepare statement, wait for a majority of replies, if one of them NACK, abort
		internal bool broadcastPrepare(PrepareRequest req)
		{
			List<Task<bool>> pendingRequests = new();
			List<Task<bool>> completedRequests = new();

			Console.WriteLine("[PrmBck] Broadcasting prepare(custmrId={0}, msgId={1}, seq={2})", req.CustomerId, req.CustomerId, req.SeqNumber);

			for (int i = 0; i < _banks.Length; i++)
			{
				PrimaryBackupService.PrimaryBackupServiceClient client = _banks[i];

				pendingRequests.Add(Task.Run(() => sendPrepare(client, req)));
			}

			// Wait for a majority of answers
			while (pendingRequests.Count >= completedRequests.Count)
			{
				int completedIndex = Task.WaitAny(pendingRequests.ToArray());
				completedRequests.Add(pendingRequests[completedIndex]);

				pendingRequests.RemoveAt(completedIndex);
			}

			// if one server answered with NACK, abort
			foreach (Task<bool> task in completedRequests)
			{
				if (!task.Result) return false;
			}
			return true;
		}

		// sends prepare request to other banks
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

		// when operation receives sequence number it's moved to commited queue, and execution is started
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

		// execute all possible operations in commited list (those who are contiguous with last)
		internal void executeAllPossible()
		{
			if (_commited.Count == 0)
				return;

			Operation headOp = _commited.Values[0];

			while (headOp.SeqNum == _lastExecuted + 1)
			{
				headOp.executeOn(_store);
				_lastExecuted = headOp.SeqNum;

				_commited.RemoveAt(0);
				_executed.Add(headOp);

				if (_commited.Count == 0)
					return;

				headOp = _commited.Values[0];
			}
		}

		// check if prepare request is valid, else reply with NACK
		internal bool canPrepare(int port, int seq)
		{
			int pid = _config.getPidFromPort(port);

			_slotLock.EnterReadLock();
			int currentPrimary = _primaryHistory[_currentSlot];
			_slotLock.ExitReadLock();

			// i) requester must be primary ii) seq must not be in commited list iii) seq must not have been executed
			return pid == currentPrimary && !_commited.ContainsKey(seq) && _lastExecuted < seq;
		}

		// (protected) get pending operations and commit them
		internal void doTakeOver()
		{
			// TODO:
			// - send ListPending to other banks
			// - everytime a reply arrives, add reply list to pending list (operations who werent already there)
			// WARNING: when we receive an operation proto we cannot create a new operation object, since it'll create a duplicate.
			//          we must find the original in the available lists and use that instead
			// - do this until majority
			// - execute 2PC to each one of them
			// - return, resuming new request processing
		}

		// return pending operations and operations since lastSeqN
		internal ListPendingReply getNewerThan(int lastSeqN)
		{
			ListPendingReply reply = new();
			SortedList<int, Operation> commitedToSend = new();  // this way we dont have to worry about inserting in correct order

			// TODO:
			// add stuff to toSend:
			// get operations from executed
			// get operations from commited

			foreach (Operation op in commitedToSend.Values)
			{
				reply.OperationList.Add(op.toProto());
			}
			foreach (Operation op in _uncommited)
			{
				reply.OperationList.Add(op.toProto());
			}

			return reply;
		}
	}
}
