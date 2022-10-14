using Common;
using Grpc.Core;
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
        // Server id
        readonly int id;

        // Addresses of learners and acceptor
        private readonly List<ServerInfo> acceptors;
        private readonly List<ServerInfo> learners;

        // Magic Fail Detector Variables
        int timeslot_ms;
        InfiniteList<bool> is_leader = new(true);


        // Proposer lock
        private readonly object proposer_lock = new object();
        // Proposer Variables
        private Thread proposer;
        private int consensusRound;
        private InfiniteList<int> numberOfTries = new InfiniteList<int>(0);
        private InfiniteList<int> proposeValue = new InfiniteList<int>(0);

        // Paxos leader detection
        private Thread fail_detector;
        private ManualResetEvent isPaxosLeaderTrigger = new ManualResetEvent(false);

        // A value has been proposed by a bank for concsensus
        private ManualResetEvent valueProposedTrigger = new ManualResetEvent(false);

        // Learner lock
        private readonly object learner_lock = new object();
        // Learners Variables
        private ManualResetEvent consensusReachedTrigger = new(false);
        private InfiniteList<InfiniteList<Tuple<int, int>>> commits;

        // Values decided in each consensus (key=round, value=consensusResult)
        private Dictionary<int, int> learned  = new();


        public Paxos(int id, List<ServerInfo> paxos_servers)
        {
            this.id = id;

            acceptors = paxos_servers;
            learners = paxos_servers;

            commits = new InfiniteList<InfiniteList<Tuple<int, int>>>(new InfiniteList<Tuple<int, int>>(new Tuple<int, int>(0, 0)));

            readConfig();

            proposer = new Thread(Proposer);
            proposer.Start();

            fail_detector = new Thread(MagicFailDetector);
            fail_detector.Start();
        }

        public int Id => id;
        public List<ServerInfo> Acceptors => acceptors;
        public List<ServerInfo> Learners => learners;

        //Consensus number = bank timeslot slot id
        public int Consensus(int newConsensus, int proposed_value)
        {
            // Check if the value has been learned
            if (learned.ContainsKey(newConsensus)) { return learned[newConsensus]; }

            lock (proposer_lock)
            {
                // TODO add capabilities
                consensusRound = newConsensus;
                proposeValue.SetItem(newConsensus, proposed_value);
            }

            valueProposedTrigger.Set();

            Console.WriteLine("[P-{0}] Main thread paused, waiting for consensus.", newConsensus);
            // wait for consensus to end
            consensusReachedTrigger.WaitOne();
            Console.WriteLine("[P-{0}] Main thread paused, Consensus reached.", newConsensus);

            // reset events (stop proposer)
            consensusReachedTrigger.Reset();
            valueProposedTrigger.Reset();

            return learned[newConsensus];
        }

        private void Proposer()
        {
            ManualResetEvent[] can_propose = { valueProposedTrigger, isPaxosLeaderTrigger };
            PaxosService.PaxosServiceClient[] clients;

            lock (acceptors)
            {
                clients = new PaxosService.PaxosServiceClient[acceptors.Count];

                for (int i = 0; i < acceptors.Count; i++)
                {
                    clients[i] = new PaxosService.PaxosServiceClient(acceptors[i].Channel);
                }
            }

            while (true)
            {
                // Wait for permission to propose
                WaitHandle.WaitAll(can_propose);

                lock (proposer_lock)
                {
                    int inst = consensusRound;
                    Task<Promise>[] pending_requests = new Task<Promise>[clients.Length];
                    List<Task<Promise>> completed_requests = new List<Task<Promise>>();

                    // Set proposal number
                    int n = id + clients.Length * numberOfTries.GetItem(inst);
                    numberOfTries[inst]++;

                    // Send prepare request to all acceptors
                    Console.WriteLine("[P-{0}] Broadcasting: prepare(n={1})", inst, n);
                    for (int i = 0; i < clients.Length; i++)
                    {
                        PaxosService.PaxosServiceClient client = clients[i];
                        // TODO perfect channel
                        pending_requests[i] = Task.Factory.StartNew<Promise>(() => client.PhaseOne(new Prepare
                        {
                            ConsensusInstance = inst,
                            N = n
                        }));
                    }

                    // Wait for a majority of answers
                    while (pending_requests.Length > completed_requests.Count)
                    {
                        int completedIndex = Task.WaitAny(pending_requests);

                        completed_requests.Add(pending_requests[completedIndex]);

                        for (int i = 0; i < pending_requests.Length - 1; i++)
                        {
                            if (i >= completedIndex) { pending_requests[i] = pending_requests[i + 1]; }
                        }

                        Array.Resize(ref pending_requests, pending_requests.Length - 1);
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
                    if (max_m > 0) { proposeValue.SetItem(inst, max_m_proposal); }

                    // Send accept requests to acceptors with proposed value
                    Accept request = new Accept
                    {
                        ConsensusInstance = inst,
                        N = n,
                        ProposedValue = proposeValue.GetItem(inst)
                    };
                    Console.WriteLine("[P-{0}] Broadcasting: accept(n={1}, val={2})", inst, n, proposeValue.GetItem(inst));

                    foreach (PaxosService.PaxosServiceClient client in clients)
                    {
                        // TODO perfect channel
                        Thread thread = new Thread(() => client.PhaseTwo(request));
                        thread.Start();
                    }

                    // Give chance for consensus to be reached
                    Thread.Sleep(timeslot_ms/4);
                }
            }
        }

        public void Learner(CommitRequest commit)
        {

            int number_of_acceptors;
            lock (acceptors) { number_of_acceptors = acceptors.Count; }

            lock (learner_lock)
            {

                int acceptor_i = commit.AcceptorId - 1;
                InfiniteList<Tuple<int, int>> current_commits = commits.GetItem(commit.ConsensusInstance);

                // if Commit is of older generation ignore else swap
                if (current_commits.GetItem(acceptor_i).Item1 > commit.CommitGeneration) { return; }
                else { current_commits.SetItem(acceptor_i, new Tuple<int, int>(commit.CommitGeneration, commit.AcceptedValue)); }

                // Check if a majority has been achieved
                int gen_count = current_commits.FindAll((tuple) => tuple.Item1 == commit.CommitGeneration).Count();
                if (gen_count < number_of_acceptors / 2) { return; }

                // Write consensus result and unblock main thread
                learned[commit.ConsensusInstance] = commit.AcceptedValue;
            }
            lock (proposer_lock)
            {
                if (commit.ConsensusInstance == consensusRound)
                    consensusReachedTrigger.Set();
            }
        }

        //Leader is assumed to be smallest non-suspected process id
        private void MagicFailDetector()
        {
            int current_timeslot = 1;

            while (true) {
                // Set (or not) self to leader 
                if (is_leader.GetItem(current_timeslot)) { isPaxosLeaderTrigger.Set(); }
                else { isPaxosLeaderTrigger.Reset(); }

                // Icrement timeslot counter
                current_timeslot++;

                // Sleep until next timeslot
                Thread.Sleep(timeslot_ms);
            }
            /*
            //TODO - Maybe add a wait for when already leader?

            // Get suspected ids from "slot" info
            List<int> possibleLeaders = otherBoneyIds.FindAll((boney) => !(suspected[slot_id].Contains(boney)));

            int smallestOther = possibleLeaders.Min();

            //If I'm the smallest "working" id, I'm the leader
            if (id < smallestOther)
                isPaxosLeaderTrigger.Set();
            else
                Thread.Sleep(500);
            */
        }

        //Prepare "schedule" for Fail Detector
        private void readConfig()
        {
            string base_path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\"));
            string config_path = Path.Combine(base_path, @"Common\config.txt");
            List<int> otherBoneyIds = new();
            Dictionary<int, List<int>> suspected = new();

            string[] lines = File.ReadAllLines(config_path);
            foreach (string line in lines)
            {
                string[] tokens = line.Split(" ");

                if (tokens.Length == 4 && tokens[0] == "P" && tokens[2] == "boney" && Int32.Parse(tokens[1]) != id)
                    otherBoneyIds.Add(Int32.Parse(tokens[1]));

                else if (tokens.Length == 2 && tokens[0] == "D")
                    timeslot_ms = Int32.Parse(tokens[1]);

                else if (tokens.Length > 1 && tokens[0] == "F")
                {
                    var tuples = Regex.Matches(line, @"[(][1-9]\d*,\s(N|F),\s(NS|S)[)]", RegexOptions.None);
                    List<int> susList = new(); //Sorting key is the same as value

                    foreach (var match in tuples)
                    {
                        string tuple = match.ToString();
                        char[] charsToTrim = { '(', ')' };
                        var info = tuple.Trim(charsToTrim);

                        //State = (PID, Frozen?, Suspected?)
                        var state = info.Split(",");
                        var pid = Int32.Parse(state[0]);

                        //Only suspect other boney processes
                        if (otherBoneyIds.Contains(pid) && state[2].Equals("S")) 
                            susList.Add(pid); 
                        
                    }

                    suspected.Add(Int32.Parse(tokens[1]), susList);
                }
            }

            foreach (int timeslot in suspected.Keys)
            {
                int leader_id = otherBoneyIds.FindAll(
                    (id) => !(suspected[timeslot].Contains(id))
                ).Min();
                is_leader.SetItem(timeslot, leader_id == id);
            }


        }

    }
}
