using BankServer.BankDomain;
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
        private float _amount;
        private int _customerId;
        private int _messageId;
        private int _seqNum;

        private float _result;
        private ManualResetEvent _executionTrigger;

        public int CustomerId { get => _customerId; set => _customerId = value; }
        public int MessageId { get => _messageId; set => _messageId = value; }
        public int SeqNum { get => _seqNum; set => _seqNum = value; }

        public Operation(OpCode opcode, float amount, int customerId, int messageId)
        {
            _opcode = opcode;
            _amount = amount;
            _customerId = customerId;
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
            op.CustomerId = CustomerId;
            op.MessageId = MessageId;
            return op;
        }

        internal float executeOn(BankStore bank)
        {
            switch (_opcode)
            {
                case OpCode.DEPOSIT:    _result = bank.Deposit(_amount); break;
                case OpCode.WITHDRAWAL: _result = bank.Withdraw(_amount); break;
                case OpCode.READ:       _result = bank.ReadBalance(); break;
                default:                throw new InvalidOperationException("Shouldn't reach this point. Unknown OpCode.");
            }
            _executionTrigger.Set();
            return _result;
        }

        public float waitForResult()
        {
            _executionTrigger.WaitOne();
            return _result;
        }

        public override string ToString()
        {
            return String.Format("Op(cust={0}, msg={1}, seq={2}, amount={3})", _customerId, _messageId, _seqNum, _amount);
        }
    }
}
