using BankServer.BankDomain;
using Common;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Bank
{
    internal class PrimaryBackup
    {
        private List<Operation> _uncommited = new();
        private PriorityQueue<Operation, int> _commited = new();
        private List<Operation> _executed = new();
        int _lastExecuted = 0;

        private BankStore _store;
        private PrimaryBackupService.PrimaryBackupServiceClient[] _banks;

        internal PrimaryBackup(BankStore store, List<string> bankAddrs, PerfectChannel perfectChannel)
        {
            _store = store;
            _banks = new PrimaryBackupService.PrimaryBackupServiceClient[bankAddrs.Count];

            int i = 0;
            foreach (string addr in bankAddrs)
            {
                _banks[i++] = new PrimaryBackupService.PrimaryBackupServiceClient(GrpcChannel.ForAddress(addr).Intercept(perfectChannel));
            }
        }

        internal void queueOperation(Operation op)
        {
            _uncommited.Add(op);

            // IF PRIMARY DO THIS {
            Thread committer = new Thread(() => Do2PhaseCommit(op));
            committer.Start();
            // }

        }

        internal void Do2PhaseCommit(Operation op)
        {
            // send message to every bank
            // wait for responses. if minority, abort
            // if majority:
            // send commit messages to every bank

        }

        internal void commitOperation(int customerId, int msgId, int seqNum)
        {
            Operation? op = _uncommited.Find(el => el.CustomerId == customerId && el.MessageId == msgId);

            if (op == null)
                return; // either already commited or unknown

            op.SeqNum = seqNum;
            _uncommited.Remove(op);
            _commited.Enqueue(op, seqNum); // move to commited queueu, with priority=seqNum

            executeAllPossible();
        }

        internal void executeAllPossible()
        {
            Operation head = _commited.Peek();

            while (head != null && head.SeqNum == _lastExecuted + 1)
            {
                head.executeOn(_store);
                _lastExecuted = head.SeqNum;

                _executed.Add(_commited.Dequeue()); // moves operation from commited to executed
                head = _commited.Peek();
            }
        }

        internal bool canPrepare(string url)
        {
            // verify if sender is really the primary for current timeslot AND? that sequence number is not already used? VERIFY LAST CONDITION
            // (im assuming we can figure it out from the url, cant we?)
            return true;
        }
    }
}
