using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankServer.BankDomain
{
    internal class BankStore
    {

        private Dictionary<int, int> balances;

        public BankStore()
        {
            balances = new Dictionary<int, int>();
        }

        public bool Deposit(int client_id, int amount) 
        {
            if (!balances.ContainsKey(client_id)) return false;

            balances[client_id] += amount;
            return true;
        }

        public bool Withrawal(int client_id, int amount)
        {
            if (!balances.ContainsKey(client_id) || balances[client_id] < amount) 
                return false;

            balances[client_id] -= amount;
            return true;
        }
        public int ReadBalance(int client_id)
        {
            if (!balances.ContainsKey(client_id))
                return -1;

            return balances[client_id];
        }



    }
}
