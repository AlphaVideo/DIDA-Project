using Common;
using Grpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boney
{
    internal class Paxos
    {
        // Server id
        int id;
        // Proposal number
        int n;

        // Addresses of learners and acceptor
        private List<ServerInfo> acceptors;
        private List<ServerInfo> learners;

        // Proposer Variables
        private Thread proposer;
        private List<int> proposed = new List<int>();

        // Paxos leader detection
        private Thread fail_detector;
        private ManualResetEvent paxos_leader = new ManualResetEvent(false);

        // A value has been proposed by a bank for concsensus
        private ManualResetEvent value_proposed = new ManualResetEvent(false);

        // Learners Variables
        private ManualResetEvent consensus_reached = new ManualResetEvent(false);
        private List<List<Tuple<int, int>>> commits = new List<List<Tuple<int, int>>>();

        // Values decided in each consensus
        private List<int> learned  = new List<int>();


        public Paxos(int id, List<ServerInfo> paxos_servers)
        {
            this.id = id;
            this.n = id;

            acceptors = paxos_servers;
            learners = paxos_servers;

            proposer = new Thread(Proposer);
            proposer.Start();

            fail_detector = new Thread(MagicFailDetector);
            fail_detector.Start();
        }

        public int Id => id;
        public List<ServerInfo> Acceptors => acceptors;
        public List<ServerInfo> Learners => learners;

        public int Consensus(int consensus_number, int proposed_value)
        {
            // Check if the value has been learned
            if (consensus_number < learned.Count) { return learned[consensus_number]; }

            // TODO [when a consensus is asked for a timestamp in the future] is this case needed?
            if (consensus_number > learned.Count) { return -1; }

            // setup proposer
            n = id;
            // TODO add capabilities
            proposed.Add(proposed_value);
            value_proposed.Set();

            // wait for consensus to end
            consensus_reached.WaitOne();

            // reset events (stop proposer)
            consensus_reached.Reset();
            value_proposed.Reset();


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

                Task<Promise>[] pending_requests = new Task<Promise>[acceptors.Count];
                List<Task<Promise>> completed_requests = new List<Task<Promise>>();
                
                // Send prepare request to all acceptors
                for (int i = 0; i < acceptors.Count; i++)
                {
                    // TODO perfect channel
                    pending_requests[i] = new Task<Promise>(() => clients[i].PhaseOne(new Prepare { N = n })); 
                    pending_requests[i].Start();
                }

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

                for (int i = 0; i < completed_requests.Count; i++)
                {
                    Promise reply = completed_requests[i].Result;

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
                // TODO better proposed list access
                if (max_m > 0) { proposed[proposed.Count - 1] = max_m_proposal; }

                // Send accept requests to acceptors with proposed value
                Accept request = new Accept
                {
                    ConsensusInstance = proposed.Count,
                    N = n,
                    ProposedValue = proposed.Last()
                };
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
                // Make sure the commits list has entries for this consensus is this necessary?
                while (!(commits.Count > commit.ConsensusInstance))
                {
                    List<Tuple<int, int>> list = new List<Tuple<int, int>>();
                    for (int i = 0; i < acceptors.Count; i++)
                    {
                        list.Add(new Tuple<int, int>(-1, -1));
                    }
                    commits.Add(list);
                }

                List<Tuple<int, int>> current_commits = commits[commit.ConsensusInstance];

                // if Commit is of older generation ignore else swap
                if (current_commits[commit.AcceptorId - 1].Item1 > commit.CommitGeneration) { return; }
                else { current_commits[commit.AcceptorId - 1] = new Tuple<int, int>(commit.CommitGeneration, commit.AcceptedValue); }

                // Check if a majority has been achieved
                int gen_count = current_commits.FindAll((tuple) => tuple.Item1 == commit.CommitGeneration).Count();
                if (gen_count < acceptors.Count / 2) { return; }

                // Make sure the learned list has entries for this consensus is this necessary?
                while (!(learned.Count > commit.ConsensusInstance))
                {
                    learned.Add(-1);
                }

                // Write consensus result and unblock main thread
                learned[commit.ConsensusInstance] = commit.AcceptedValue;
                consensus_reached.Set();
            }
        }

        private void MagicFailDetector()
        {
            // TODO how to detect if this process must be a leader and propose a value?
            if (id == 1)
            {
                while (true)
                {
                    Thread.Sleep(500);
                    paxos_leader.Set();
                }
            }
        }




    }
}
