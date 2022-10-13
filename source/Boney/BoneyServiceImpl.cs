using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boney
{
    internal class BoneyServiceImpl : BoneyService.BoneyServiceBase
    {
        private Paxos _paxos;
        public BoneyServiceImpl(Paxos paxos)
        {
            _paxos = paxos;
        }
        public override Task<CompareSwapReply> CompareAndSwap(CompareSwapRequest request, ServerCallContext context)
        {
            CompareSwapReply reply = new();

            // TODO frozen state

            reply.Outvalue = _paxos.Consensus(request.Slot, request.Invalue);


            return Task.FromResult(reply);
        }
    }
}
