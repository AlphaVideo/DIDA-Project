using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using BankServer.BankDomain;

namespace BankServer.Services
{
    internal class BankServiceImpl : BankService.BankServiceBase
    {

        private BankStore _store;

        public BankServiceImpl(BankStore store) 
        { 
            _store = store;
        }
        public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
        {
            var reply = new DepositReply();
            reply.Status = _store.Deposit(request.Amount);
            return Task.FromResult(reply);
        }

        public override Task<WithdrawalReply> Withdrawal(WithdrawalRequest request, ServerCallContext context)
        {
            var reply = new WithdrawalReply();
            reply.Status = _store.Withdrawal(request.Amount);
            return Task.FromResult(reply);
        }

        public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
        {
            var reply = new ReadBalanceReply();
            reply.Balance = _store.ReadBalance();
            return Task.FromResult(reply);
        }

    }
}
