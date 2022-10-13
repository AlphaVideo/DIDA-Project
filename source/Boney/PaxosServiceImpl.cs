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

        enum PROMISE_STATUS
        {
            NOT_PROMISED,
            PROMISED,
            ACCEPTED
        }

        // Acceptor status for each consensus instance
        private InfiniteList<PROMISE_STATUS> promise_statuses = new InfiniteList<PROMISE_STATUS>(PROMISE_STATUS.NOT_PROMISED);

        // Promised generation numbers
        private InfiniteList<int> promised_ids = new InfiniteList<int>(-1);

        // Last value accepted and commited for each consensus instance
        private InfiniteList<int> accepted_values = new InfiniteList<int>(-1);

        // Paxos object
        private Paxos paxos;

        public PaxosServiceImpl(Paxos paxos)
        {
            this.paxos = paxos;
        }


        public override Task<Promise> PhaseOne(Prepare prepare, ServerCallContext context)
        {
            lock (this) 
            { 
                int inst = prepare.ConsensusInstance;
                PROMISE_STATUS status = promise_statuses.GetItem(inst);

                Console.WriteLine("PaxosService LINE 48");
                // First proposal for this instance
                if (status == PROMISE_STATUS.NOT_PROMISED)
                {
                    Console.WriteLine("PaxosService LINE 52");
                    promised_ids.SetItem(inst, prepare.N);
                    promise_statuses.SetItem(inst, PROMISE_STATUS.PROMISED);

                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.First
                    });
                }

                // Ignore proposal
                else if (prepare.N < promised_ids.GetItem(inst))
                {
                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.Nack
                    });
                }

                // Later id but only promised was made
                else if (status == PROMISE_STATUS.PROMISED)
                {
                    int prev_id = promised_ids.GetItem(inst);
                    promised_ids.SetItem(inst, prepare.N);
                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.PrevPromised,
                        M = prev_id
                    });
                }

                // Later id and already accepted value
                else
                {
                    int prev_id = promised_ids.GetItem(inst);
                    promised_ids.SetItem(inst, prepare.N);
                    // TODO PROMISED?
                    status = PROMISE_STATUS.PROMISED;

                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.PrevAccepted,
                        M = prev_id,
                        PrevProposedValue = accepted_values.GetItem(inst)
                    });
                }
            }
        }

        public override Task<EmptyReply> PhaseTwo(Accept accept, ServerCallContext context)
        {
            lock (this)
            {
                int inst = accept.ConsensusInstance;
                if (promise_statuses.GetItem(inst) == PROMISE_STATUS.NOT_PROMISED || accept.N >= promised_ids.GetItem(inst))
                {
                    accepted_values.SetItem(inst, accept.ProposedValue);
                    promise_statuses.SetItem(inst, PROMISE_STATUS.ACCEPTED);

                    CommitRequest commit = new CommitRequest
                    {
                        ConsensusInstance = inst,
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
