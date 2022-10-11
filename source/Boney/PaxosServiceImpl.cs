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

        private List<PROMISE_STATUS> promise_statuses = new List<PROMISE_STATUS>();
        private List<int> promised_ids = new List<int>();
        private List<int> accepted_values = new List<int>();

        private Paxos paxos;

        public PaxosServiceImpl(Paxos paxos)
        {
            this.paxos = paxos;
        }


        public override Task<Promise> PhaseOne(
        Prepare prepare, ServerCallContext context)
        {
            lock (this) 
            { 
                int inst = prepare.ConsensusInstance;
                PROMISE_STATUS status = promise_statuses[inst];

                // First proposal for this instance
                if (status == PROMISE_STATUS.NOT_PROMISED)
                {
                    promised_ids[inst] = prepare.N;
                    promise_statuses[inst] = PROMISE_STATUS.PROMISED;

                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.First
                    });
                }

                // Ignore proposal
                else if (prepare.N < promised_ids[inst])
                {
                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.Nack
                    });
                }

                // Later id but only promised was made
                else if (status = PROMISE_STATUS.PROMISED)
                {
                    int prev_id = promised_ids[inst];
                    promised_ids[inst] = prepare.N;
                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.PrevPromised,
                        M = prev_id
                    });
                }

                // Later id and already accepted value
                else
                {
                    int prev_id = promised_ids[inst];
                    promised_ids[inst] = prepare.N;
                    return Task.FromResult(new Promise
                    {
                        Status = Promise.Types.PROMISE_STATUS.PrevAccepted,
                        M = prev_id,
                        PrevProposedValue = accepted_values[inst]
                    });
                }
            }
        }

        public override Task<EmptyReply> PhaseTwo(Accept accept, ServerCallContext context)
        {
            lock (this)
            {
                int inst = accept.ConsensusInstance;
                if (promise_statuses[inst] == PROMISE_STATUS.NOT_PROMISED || accept.N >= promised_ids[inst])
                {
                    accepted_values[inst] = accept.ProposedValue;
                    promise_statuses[inst] = PROMISE_STATUS.ACCEPTED;

                    foreach (ServerInfo learner in paxos.learners)
                    {
                        Thread thread = new Thread(() => paxos.SendCommit(learner, accept.ProposedValue));
                        thread.Start();
                    }
                }

                return Task.FromResult(new EmptyReply());
            }
        }

        public override Task<EmptyReply> Commit(CommitRequest commit, ServerCallContext context)
        {

            return Task.FromResult(new EmptyReply());
        }

        private void SendCommit(ServerInfo server_to_contact, int value_to_commit)
        {
            // Send CommitRequest to server
        }
    }
}
