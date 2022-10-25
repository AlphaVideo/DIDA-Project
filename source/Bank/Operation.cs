using System;
using System.Collections.Generic;
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

        public Operation(OpCode opcode, int amount, int clientId, int messageId)
        {
            _opcode = opcode;
            _amount = amount;
            _clientId = clientId;
            _messageId = messageId;
        }

        public ProtoOperation toProto()
        {
            ProtoOperation op = new();

            switch (_opcode)
            {
                case OpCode.DEPOSIT:  op.Operation = ProtoOperation.Types.OpCode.Deposit; op.Amount = _amount;  break;
                case OpCode.WITHDRAWAL: op.Operation = ProtoOperation.Types.OpCode.Withdraw; op.Amount = _amount; break;
                case OpCode.READ: op.Operation = ProtoOperation.Types.OpCode.Read; break;
            }
            op.ClientId = _clientId;
            op.MessageId = _messageId;
            return op;
        }
    }
}
