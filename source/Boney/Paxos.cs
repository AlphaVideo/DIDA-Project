using Common;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client.Balancer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Boney
{
	internal class Paxos
	{
		//Config
		static Config config = new();
		private Timeslots timeslots = config.getTimeslots();

		// Server id
		readonly int id;

		// Channels to learners/acceptors
		PaxosService.PaxosServiceClient[] paxosClients;

		// Magic Fail Detector Variables
		int timeslot_ms = config.getSlotDuration();
		int maxSlots = config.getSlotCount();
		bool hasEnded = false;
		InfiniteList<bool> is_leader = new(false);

		// Startup time
		DateTime _startuptTime;

		// Proposer lock
		private readonly object proposer_lock = new object();
		// Proposer Variables
		private Thread proposer;
		private int consensusRound;
		private int numberOfTries;
		private Dictionary<int, int> proposeValue = new();

		// Paxos leader detection
		private Thread fail_detector;
		private ManualResetEvent isPaxosLeaderTrigger = new ManualResetEvent(false);

		// A value has been proposed by a bank for concsensus
		private ManualResetEvent valueProposedTrigger = new ManualResetEvent(false);

		// Learner lock
		private readonly object learner_lock = new object();
		// Learners Variables
		private ManualResetEvent consensusReachedTrigger = new(false);
		private CommitHistory commits = new();

		// Values decided in each consensus (key=round, value=consensusResult)
		private Dictionary<int, int> learned  = new();


		public Paxos(int id, List<ServerInfo> paxos_servers, PerfectChannel perfectChannel, DateTime startupTime)
		{
			this.id = id;

			_startuptTime = startupTime;

			calculateLeaders();

			// Prepare paxos client for acceptors
			paxosClients = new PaxosService.PaxosServiceClient[paxos_servers.Count];
			for (int i = 0; i < paxos_servers.Count; i++)
			{
				paxosClients[i] = new PaxosService.PaxosServiceClient(paxos_servers[i].Channel.Intercept(perfectChannel));
			}

			// Start fail detector thread
			fail_detector = new Thread(MagicFailDetector);
			fail_detector.Start();

			//start proposer thread
			proposer = new Thread(Proposer);
			proposer.Start();

		}

		public int Id => id;
		public PaxosService.PaxosServiceClient[] PaxosClients => paxosClients;

		//Consensus number = bank timeslot slot id
		public int Consensus(int newConsensus, int proposed_value)
		{
			lock (learner_lock)
			{
				// Check if the value has been learned
				if (learned.ContainsKey(newConsensus)) { return learned[newConsensus]; }
			}

			lock (proposer_lock)
			{
				consensusRound = newConsensus;
				numberOfTries = 0;
				if (!proposeValue.ContainsKey(consensusRound))
					proposeValue[consensusRound] = proposed_value;
			}

			valueProposedTrigger.Set();

			Console.WriteLine("[Paxos ] Main thread paused, waiting for consensus.");
			// wait for consensus to end
			consensusReachedTrigger.WaitOne();
			Console.WriteLine("[Paxos ] Consensus reached.");

			// reset events (stop proposer)
			consensusReachedTrigger.Reset();
			valueProposedTrigger.Reset();

			lock (learner_lock)
			{
				return learned[newConsensus];
			}
		}

		private void Proposer()
		{
			ManualResetEvent[] canProposeTrigger = { valueProposedTrigger, isPaxosLeaderTrigger };

			while (true)
			{
				// Wait for permission to propose
				WaitHandle.WaitAll(canProposeTrigger);

				lock (proposer_lock)
				{
					List<Task<Promise>> pending_requests = new();
					List<Task<Promise>> completed_requests = new();

					// Set proposal number
					int n = id + paxosClients.Length * numberOfTries;
					numberOfTries++;

					var req = new Prepare{ConsensusInstance = consensusRound, N = n};

					// Send prepare request to all acceptors
					Console.WriteLine("[Propsr] Broadcasting: prepare(n={0})", n);
					for (int i = 0; i < paxosClients.Length; i++)
					{
						PaxosService.PaxosServiceClient client = paxosClients[i];
						pending_requests.Add(Task.Run(() => client.PhaseOne(req)));
					}

					// Wait for a majority of answers
					while (pending_requests.Count > completed_requests.Count)
					{
						int completedIndex = Task.WaitAny(pending_requests.ToArray());
						completed_requests.Add(pending_requests[completedIndex]);

						pending_requests.RemoveAt(completedIndex);
					}

					// Process promises
					int max_m = 0;
					int max_m_proposal = 0;
					bool end_proposal = false;
					for (int i = 0; i < completed_requests.Count; i++)
					{
						var task = completed_requests[i];
						var reply = task.Result;


						switch (reply.Status)
						{
							case Promise.Types.PROMISE_STATUS.Nack:
								end_proposal = true;
								break;
							case Promise.Types.PROMISE_STATUS.PrevAccepted:
								if (reply.M > max_m)
								{
									max_m = reply.M; 
									max_m_proposal = reply.PrevProposedValue;
								}
								break;
						}
					}

					if (end_proposal) { isPaxosLeaderTrigger.Reset(); continue; }
					if (max_m > 0) { proposeValue[consensusRound] = max_m_proposal; }

					// Send accept requests to acceptors with proposed value
					Accept request = new Accept
					{
						ConsensusInstance = consensusRound,
						N = n,
						ProposedValue = proposeValue[consensusRound]
					};
					Console.WriteLine("[Propsr] Broadcasting: accept(n={0}, val={1})", n, proposeValue[consensusRound]);

					foreach (PaxosService.PaxosServiceClient client in paxosClients)
					{
						Thread thread = new Thread(() => client.PhaseTwo(request));
						thread.Start();
					}
				}

				// Give chance for consensus to be reached
				Random rand = new();
				Thread.Sleep(timeslot_ms / 16 * rand.Next(1, 14));

				if (hasEnded)
					return;
			}
		}

		public void Learner(CommitRequest request)
		{
			// ignore if we have already learnt the value for that instance
			if (learned.ContainsKey(request.ConsensusInstance))
				return;

			lock (learner_lock)
			{
				Dictionary<int, Commit> current_commits = commits[request.ConsensusInstance];

				// if commit exists and is of older generation then ignore, else swap
				if (current_commits.ContainsKey(request.AcceptorId) && current_commits[request.AcceptorId].Generation > request.CommitGeneration) {
					return;
				}
				else {
					current_commits[request.AcceptorId] = new Commit(request.CommitGeneration, request.AcceptedValue);
				}

				// Check if a majority has been achieved
				int gen_count = current_commits.Where((commit) => commit.Value.Generation == request.CommitGeneration).Count();
				if (gen_count <= paxosClients.Length / 2) { return; }

				// Write consensus result and unblock main thread
				learned[request.ConsensusInstance] = request.AcceptedValue;
			}
			if (request.ConsensusInstance == consensusRound)
				consensusReachedTrigger.Set();
		}

		//Leader is assumed to be smallest non-suspected process id
		private void MagicFailDetector()
		{
			int current_timeslot = 1;

			Thread.Sleep(_startuptTime - DateTime.Now); 

			while (current_timeslot <= maxSlots) {
				// Set (or not) self to leader 
				if (is_leader.GetItem(current_timeslot)) {
					Console.WriteLine("[Paxos ] I'm the LEADER for slot {0}", current_timeslot);
					isPaxosLeaderTrigger.Set();
				}
				else {
					Console.WriteLine("[Paxos ] I'm NOT the leader for slot {0}", current_timeslot);
					isPaxosLeaderTrigger.Reset();
				}

				// Icrement timeslot counter
				current_timeslot++;

				// Sleep until next timeslot
				Thread.Sleep(timeslot_ms);
			}
			Console.WriteLine("Last timeslot ({0}) has ENDED", current_timeslot - 1);
			hasEnded = true;
		}

		//Prepare "schedule" for Fail Detector
		private void calculateLeaders()
		{
			for (int timeslot = 1; timeslot <= maxSlots; timeslot++)
			{
				List<int> candidateLeaders = new List<int>(config.getBoneyIds());

                foreach (int sus in timeslots.getMySuspectList(timeslot, id))
					candidateLeaders.Remove(sus);
				
				is_leader.SetItem(timeslot, candidateLeaders.Min() == id);
			}
		}
	}
}
