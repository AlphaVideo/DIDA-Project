using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using BankServer.BankDomain

namespace BankServer.Services
{
    internal class BankServiceImpl : BankService.BankServiceBase
    {

        private BankStore store;

        public BankServiceImpl(BankStore store) 
        { 
            this.store = store;
        }

        public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
        {
            return Task.FromResult(request);
        }

        public override Task<WithrawalReply> Withrawal(WithrawalRequest request, ServerCallContext context)
        {
            return Task.FromResult(request);
        }

        public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
        {
            return Task.FromResult(request);
        }

    }
}
