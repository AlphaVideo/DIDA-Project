using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boney
{
    internal class Paxos
    {


        private int proposed_value = -1;
        private ConcurrentDictionary<int, int> commited_values = new ConcurrentDictionary<int, int>();
        private ConcurrentDictionary<int, int> learned_values = new ConcurrentDictionary<int, int>();

        public Paxos()
        {

        }

        public int Consensus(int consensus_number, int proposed_value)
        {

        }

    }
}
