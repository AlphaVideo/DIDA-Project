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

        public int Deposit(int amount) 
        {
            return _balance += amount;
        }

        public int Withdraw(int amount)
        {
            if (_balance >= amount)
                return _balance -= amount;
            else
                return 0;
        }
        public int ReadBalance()
        {
            return _balance;
        }
    }
}
