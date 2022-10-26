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
        private PrimaryBackup _datacentre;

        public BankServiceImpl(PrimaryBackup datacentre)
        { 
            _datacentre = datacentre;
        }


        public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
        {
            var reply = new DepositReply();

            Operation op = new(Operation.OpCode.DEPOSIT, request.Amount, request.CustomerId, request.MsgId);
            _datacentre.queueOperation(op);

            reply.Balance = op.waitForResult();

            return Task.FromResult(reply);
        }

        public override Task<WithdrawalReply> Withdrawal(WithdrawalRequest request, ServerCallContext context)
        {
            var reply = new WithdrawalReply();

            Operation op = new(Operation.OpCode.WITHDRAWAL, request.Amount, request.CustomerId, request.MsgId);
            _datacentre.queueOperation(op);

            reply.Balance = op.waitForResult();

            return Task.FromResult(reply);
        }

        public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
        {
            var reply = new ReadBalanceReply();

            Operation op = new(Operation.OpCode.READ, 0, request.CustomerId, request.MsgId);
            _datacentre.queueOperation(op);

            reply.Balance = op.waitForResult();

            return Task.FromResult(reply);
        }

    }
}
