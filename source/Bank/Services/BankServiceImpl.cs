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
            reply.Status = _isRunning;

            if (_isRunning) // create and add request to queue
                ;

            return Task.FromResult(reply);
        }

        public override Task<WithdrawalReply> Withdrawal(WithdrawalRequest request, ServerCallContext context)
        {
            var reply = new WithdrawalReply();
            reply.Status = _isRunning;

            if (_isRunning) // create and add request to queue
                ;

            return Task.FromResult(reply);
        }

        public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
        {
            var reply = new ReadBalanceReply();
            reply.Status = _isRunning;

            // send balance only if it's running
            if (_isRunning)
                reply.Balance = _store.ReadBalance();

            return Task.FromResult(reply);
        }

    }
}
