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
            int val;
            CompareSwapReply reply = new();

            // TODO frozen state
            Console.WriteLine("[Boney ] Received CaS for slot {0} with value {1}", request.Slot, request.Invalue);
            val = _paxos.Consensus(request.Slot, request.Invalue);
            Console.WriteLine("[Boney ] Reached consensus with value {0}", val);

            reply.Outvalue = val;

            return Task.FromResult(reply);
        }
    }
}
