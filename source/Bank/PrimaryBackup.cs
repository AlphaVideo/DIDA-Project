using BankServer.BankDomain;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private PerfectChannel _perfectChannel;

        internal PrimaryBackup(BankStore store, PerfectChannel perfectChannel)
        {
            _store = store;
            _perfectChannel = perfectChannel;
        }

        internal void queueOperation(Operation op)
        {
            _uncommited.Add(op);

            // IF PRIMARY
            Thread committer = new Thread(() => Do2PhaseCommit(op));
            committer.Start();

        }

        internal void Do2PhaseCommit(Operation op)
        {

        }
    }
}
