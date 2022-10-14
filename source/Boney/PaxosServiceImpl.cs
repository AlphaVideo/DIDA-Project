using Common;
using Grpc.Core;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boney
{
    internal class PaxosServiceImpl : PaxosService.PaxosServiceBase
    {
        // Acceptor lock
        private readonly object acceptor_lock = new object();
        // Read and write timestamps for the acceptor
        private InfiniteList<int> read_timestamps = new InfiniteList<int>(0);
        private InfiniteList<Tuple<int, int>> write_timestamps = new InfiniteList<Tuple<int, int>>(new Tuple<int, int>(0, 0)); // (Item1, Item2) := (timestamp, value)

        // Paxos object
        private Paxos paxos;

        public PaxosServiceImpl(Paxos paxos)
        {
            this.paxos = paxos;
        }


        public override Task<Promise> PhaseOne(Prepare prepare, ServerCallContext context)
        {
            lock (acceptor_lock)
            {
                // Read and write timestamps for this consensus instance
                int read_timestamp = read_timestamps.GetItem(prepare.ConsensusInstance);
                Tuple<int, int> write_timestamp = write_timestamps.GetItem(prepare.ConsensusInstance);

                Console.WriteLine("[PaxImp] Received prepare(n={0})", prepare.N);
                // Ignore proposal
                if (prepare.N < read_timestamp)
                {
                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.Nack
                    });
                }

                // New read timestamp and no previously accepted value
                else if (read_timestamp < prepare.N && write_timestamp.Item1 == 0)
                {
                    read_timestamps.SetItem(prepare.ConsensusInstance, prepare.N);

                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.First
                    });
                }

                // New read timestamp and had previously accepted a value
                else
                {
                    int prev_timestamp = read_timestamp;
                    read_timestamps.SetItem(prepare.ConsensusInstance, prepare.N);

                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.PrevAccepted,
                        M = prev_timestamp,
                        PrevProposedValue = write_timestamp.Item2
                    });
                }
            }
        }

        public override Task<EmptyReply> PhaseTwo(Accept accept, ServerCallContext context)
        {
            Console.WriteLine("[PaxImp] Received accept(n={0}, val={1})", accept.N, accept.ProposedValue);

            lock (acceptor_lock)
            {
                if (accept.N >= read_timestamps.GetItem(accept.ConsensusInstance))
                {
                    // Set read and write timestamps to new value
                    read_timestamps.SetItem(accept.ConsensusInstance, accept.N);
                    write_timestamps.SetItem(accept.ConsensusInstance, new Tuple<int, int>(accept.N, accept.ProposedValue));

                    // Create and broadcast CommitRequest to all learners
                    CommitRequest commit = new CommitRequest
                    {
                        ConsensusInstance = accept.ConsensusInstance,
                        CommitGeneration = accept.N,
                        AcceptorId = paxos.Id,
                        AcceptedValue = accept.ProposedValue
                    };

                    foreach (ServerInfo learner in paxos.Learners)
                    {
                        Thread thread = new Thread(() => SendCommit(learner, commit));
                        thread.Start();
                    }
                }

                return Task.FromResult(new EmptyReply());
            }
        }

        public override Task<EmptyReply> Commit(CommitRequest commit, ServerCallContext context)
        {
            paxos.Learner(commit);
            return Task.FromResult(new EmptyReply());
        }

        private void SendCommit(ServerInfo server_to_contact, CommitRequest commit)
        {
            PaxosService.PaxosServiceClient client = new PaxosService.PaxosServiceClient(server_to_contact.Channel);
            client.Commit(commit);
        }
    }
}
