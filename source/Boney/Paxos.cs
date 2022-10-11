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
        int id;
        int n;
        private List<ServerInfo> acceptors { get; }
        private List<ServerInfo> learners { get; }

        private ManualResetEvent paxos_leader = new ManualResetEvent(false);
        private List<int> proposed = new List<int>();
        private Thread proposer;

        private ManualResetEvent value_proposed = new ManualResetEvent(false);
        private Thread fail_detector;


        private ManualResetEvent consensus_reached = new ManualResetEvent(false);
        private List<List<int>> commits = new List<List<int>>();
        private List<int> learned  = new List<int>();


        public Paxos(int id)
        {
            this.id = id;
            this.n = id;

            proposer = new Thread(() => Proposer());
            proposer.start();

            fail_detector = new Thread(() => MagicFailDetector());
            fail_detector.start();
        }

        public int Consensus(int consensus_number, int proposed_value)
        {
            // Check if the value has been learned
            if (consensus_number < learned.Count) { return learned[consensus_number]; }

            // TODO [when a consensus is asked for a timestamp in the future] is this case needed?
            if (consensus_number != learned.Count) { return -1; }

            // setup proposer
            n = id;
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
            PaxosService.PaxosServiceClient[] clients = PaxosService.PaxosServiceClient[acceptors.Count];

            for (int i = 0; i < acceptors.Count; i++)
            {
                clients[i] = new PaxosService.PaxosServiceClient(acceptor_info.GetChannel());
            }

            while (true)
            {
                // Wait for permission to propose
                WaitHandle.WaitAll(can_propose);

                Task<Promise>[] pending_requests = Task<Promise>[acceptors.Count];
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
                int max_m_proposal;
                bool end_proposal = false;

                for (int i = 0; i < completed_requests.Count; i++)
                {
                    Promise reply = completed_requests[i].Result;

                    switch (reply.Status)
                    {
                        case Promise.Types.PROMISE_STATUS.Nack:
                            end_proposal = true;
                        case Promise.Types.PROMISE_STATUS.PrevAccepted:
                            if (reply.M > max_M)
                            {
                                max_m = reply.M; 
                                max_m_proposal = reply.PrevProposedValue;
                            }
                    }
                }

                if (end_proposal) { continue; }
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
                    ThreadPool.QueueUserWorkItem(() => client.PhaseTwo(request));
                }
                paxos_leader.Reset();
            }
        }

        private void MagicFailDetector()
        {
            // TODO how to detect if this process must be a leader and propose a value?
            if (id == 1)
            {
                paxos_leader.Set();
            }
        }




    }
}
