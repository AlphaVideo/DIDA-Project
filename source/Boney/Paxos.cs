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
        int id;
        // Proposal number
        int n;
        //Fail detection list: SlotID -> Sus IDs 
        Dictionary<int, List<int>> suspected = new();
        List<int> otherBoneyIds = new();

        // slot id
        int slot_id = 1;

        // Addresses of learners and acceptor
        private readonly List<ServerInfo> acceptors;
        private readonly List<ServerInfo> learners;

        // Proposer Variables
        private Thread proposer;
        private int proposer_consensus;
        private InfiniteList<int> proposed = new InfiniteList<int>(-1);

        // Paxos leader detection
        private Thread fail_detector;
        private ManualResetEvent paxos_leader = new ManualResetEvent(false);

        // A value has been proposed by a bank for concsensus
        private ManualResetEvent value_proposed = new ManualResetEvent(false);

        // Learners Variables
        private ManualResetEvent consensus_reached = new ManualResetEvent(false);
        private InfiniteList<InfiniteList<Tuple<int, int>>> commits;

        // Values decided in each consensus
        private InfiniteList<int> learned  = new InfiniteList<int>(-1);


        public Paxos(int id, List<ServerInfo> paxos_servers)
        {
            this.id = id;
            this.n = id;

            acceptors = paxos_servers;
            learners = paxos_servers;

            commits = new InfiniteList<InfiniteList<Tuple<int, int>>>(new InfiniteList<Tuple<int, int>>(new Tuple<int, int>(-1, -1)));

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
        public int Consensus(int consensus_number, int proposed_value)
        {
            slot_id = consensus_number;
            // Check if the value has been learned
            if (consensus_number < learned.Count) { return learned[consensus_number]; }

            // TODO [when a consensus is asked for a timestamp in the future] is this case needed?
            //if (consensus_number > learned.Count) { return -1; }

            // setup proposer
            n = id;
            // TODO add capabilities
            proposer_consensus = consensus_number;
            proposed.SetItem(consensus_number, proposed_value);
            value_proposed.Set();

            Console.WriteLine("[P] Main thread paused, waiting for consensus.");
            // wait for consensus to end
            consensus_reached.WaitOne();
            Console.WriteLine("[P] Consensus reached.");

            // reset events (stop proposer)
            consensus_reached.Reset();
            value_proposed.Reset();

            foreach (int i in learned)
            {
                Console.Write("{0}, ", i);
            }

            return learned[consensus_number];
        }

        private void Proposer()
        {
            ManualResetEvent[] can_propose = { value_proposed, paxos_leader };
            PaxosService.PaxosServiceClient[] clients = new PaxosService.PaxosServiceClient[acceptors.Count];

            for (int i = 0; i < acceptors.Count; i++)
            {
                clients[i] = new PaxosService.PaxosServiceClient(acceptors[i].Channel);
            }

            while (true)
            {
                // Wait for permission to propose
                WaitHandle.WaitAll(can_propose);

                int inst = proposer_consensus;
                Task<Promise>[] pending_requests = new Task<Promise>[acceptors.Count];
                List<Task<Promise>> completed_requests = new List<Task<Promise>>();

                // Send prepare request to all acceptors
                Console.WriteLine("[P] Broadcasting: prepare(n={0})", n);
                for (int i = 0; i < acceptors.Count; i++)
                {
                    Console.WriteLine("Paxos LINE 131");
                    // TODO perfect channel
                    pending_requests[i] = new Task<Promise>(() => clients[i].PhaseOne(new Prepare { N = n })); 
                    pending_requests[i].Start();
                }

                Console.WriteLine("Paxos LINE 137");
                // Wait for a majority of answers
                while (pending_requests.Length > completed_requests.Count)
                {
                    int i_completed = Task.WaitAny(pending_requests);

                    completed_requests.Add(pending_requests[i_completed]);

                    for (int i = 0; i < pending_requests.Length-1; i++)
                    {
                        if (i >= i_completed) { pending_requests[i] = pending_requests[i + 1]; }
                    }

                    Array.Resize(ref pending_requests, pending_requests.Length-1);
                }

                // Process promises
                int max_m = -1;
                int max_m_proposal = 0;
                bool end_proposal = false;
                Console.WriteLine("Completed requests: " + completed_requests.Count);
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

                // TODO better leader selection policy
                if (end_proposal) { paxos_leader.Reset();  continue; }
                if (max_m > 0) { proposed.SetItem(inst, max_m_proposal); }

                // Send accept requests to acceptors with proposed value
                Accept request = new Accept
                {
                    ConsensusInstance = inst,
                    N = n,
                    ProposedValue = proposed.GetItem(inst)
                };
                Console.WriteLine("[P] Broadcasting: accept(n={0}, val={1})", n, proposed.GetItem(inst));

                foreach (PaxosService.PaxosServiceClient client in clients)
                {
                    // TODO perfect channel
                    Thread thread = new Thread(() => client.PhaseTwo(request));
                    thread.Start();
                }

                // TODO must create better leader selection policy
                paxos_leader.Reset();
            }
        }

        public void Learner(CommitRequest commit)
        {
            lock (this)
            {

                int acceptor_i = commit.AcceptorId - 1;
                InfiniteList<Tuple<int, int>> current_commits = commits.GetItem(commit.ConsensusInstance);

                // if Commit is of older generation ignore else swap
                if (current_commits.GetItem(acceptor_i).Item1 > commit.CommitGeneration) { return; }
                else { current_commits.SetItem(acceptor_i, new Tuple<int, int>(commit.CommitGeneration, commit.AcceptedValue)); }

                // Check if a majority has been achieved
                int gen_count = current_commits.FindAll((tuple) => tuple.Item1 == commit.CommitGeneration).Count();
                if (gen_count < acceptors.Count / 2) { return; }

                // Write consensus result and unblock main thread
                learned.SetItem(commit.ConsensusInstance, commit.AcceptedValue);
                consensus_reached.Set();
            }
        }

        //Leader is assumed to be smallest non-suspected process id
        private void MagicFailDetector()
        {
            //TODO - Maybe add a wait for when already leader?
            
            List<int> possibleLeaders = new List<int>(otherBoneyIds);

            foreach (int suspect in suspected[slot_id]) //Get suspected ids from "slot" info
            {
                possibleLeaders.Remove(suspect);
            }

            int smallestOther = possibleLeaders.Min();

            //If I'm the smallest "working" id, I'm the leader
            if (id < smallestOther)
                paxos_leader.Set();
            else
                Thread.Sleep(500);
        }

        //Prepare "schedule" for Fail Detector
        private void readConfig()
        {
            string base_path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\"));
            string config_path = Path.Combine(base_path, @"Common\config.txt");

            string[] lines = File.ReadAllLines(config_path);
            foreach (string line in lines)
            {
                string[] tokens = line.Split(" ");

                if (tokens.Length == 4 && tokens[0] == "P" && tokens[2] == "boney" && Int32.Parse(tokens[1]) != id)
                    otherBoneyIds.Add(Int32.Parse(tokens[1]));

                if (tokens.Length > 1 && tokens[0] == "F")
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
        }

    }
}
