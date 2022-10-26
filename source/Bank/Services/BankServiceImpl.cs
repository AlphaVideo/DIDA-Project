using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using BankServer.BankDomain;
using Bank;

namespace BankServer.Services
{
    internal class BankServiceImpl : BankService.BankServiceBase
    {

        private BankStore _store;
        private bool _isRunning;

        public BankServiceImpl(BankStore store) 
        { 
            _store = store;
            _isRunning = true;
        }

        public bool ToggleIsRunning()
        {
            _isRunning = ! _isRunning;
            return _isRunning;
        }

        public void setIsRunning(bool b)
        {
            _isRunning = b;
        }

        public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
        {
            var reply = new DepositReply();

            Operation op;

            // create operation based on request
            // store operation in appropriate queue
            // if this is the primary process, initiate 2-phase commit

            reply.Balance = op.waitForResult();

            return Task.FromResult(reply);
        }

        public override Task<WithdrawalReply> Withdrawal(WithdrawalRequest request, ServerCallContext context)
        {
            var reply = new WithdrawalReply();

            Operation op;

            // create operation based on request
            // store operation in appropriate queue
            // if this is the primary process, initiate 2-phase commit

            reply.Balance = op.waitForResult();

            return Task.FromResult(reply);
        }

        public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
        {
            var reply = new ReadBalanceReply();

            Operation op;

            // create operation based on request
            // store operation in appropriate queue
            // if this is the primary process, initiate 2-phase commit

            reply.Balance = op.waitForResult();

            return Task.FromResult(reply);
        }

    }
}
