using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankServer.BankDomain
{
    internal class BankStore
    {
        private int _balance; //only one client supported

        public BankStore()
        {
            _balance = 0;
        }

        public bool Deposit(int amount) 
        {
            _balance += amount;
            return true;
        }

        public bool Withdrawal(int amount)
        {
            if (_balance < amount) 
                return false;

            _balance -= amount;
            return true;
        }
        public int ReadBalance()
        {
            return _balance;
        }
    }
}
