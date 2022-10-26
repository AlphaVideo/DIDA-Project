﻿using BankServer.BankDomain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bank
{
    public class Operation
    {
        public enum OpCode
        {
            DEPOSIT,
            WITHDRAWAL,
            READ
        }

        private OpCode _opcode;
        private int _amount;
        private int _clientId;
        private int _messageId;
        private int _seqNum;

        private int _result;
        private ManualResetEvent _executionTrigger;

        public int SeqNum { get => _seqNum; set => _seqNum = value; }

        public Operation(OpCode opcode, int amount, int clientId, int messageId)
        {
            _opcode = opcode;
            _amount = amount;
            _clientId = clientId;
            _messageId = messageId;
            _seqNum = -1;

            _executionTrigger = new ManualResetEvent(false);
        }

        public ProtoOperation toProto()
        {
            ProtoOperation op = new();

            switch (_opcode)
            {
                case OpCode.DEPOSIT:    op.Operation = ProtoOperation.Types.OpCode.Deposit;  op.Amount = _amount; break;
                case OpCode.WITHDRAWAL: op.Operation = ProtoOperation.Types.OpCode.Withdraw; op.Amount = _amount; break;
                case OpCode.READ:       op.Operation = ProtoOperation.Types.OpCode.Read; break;
            }
            op.ClientId = _clientId;
            op.MessageId = _messageId;
            return op;
        }

        internal int executeOn(BankStore bank)
        {switch (_opcode)
            {
                case OpCode.DEPOSIT:    _result = bank.Deposit(_amount); break;
                case OpCode.WITHDRAWAL: _result = bank.Withdraw(_amount); break;
                case OpCode.READ:       _result = bank.ReadBalance(); break;
                default:                throw new InvalidOperationException("Shouldn't reach this point. Unknown OpCode.");
            }
            _executionTrigger.Set();
            return _result;
        }

        public void waitExecution()
        {
            _executionTrigger.WaitOne();
        }
    }
}
