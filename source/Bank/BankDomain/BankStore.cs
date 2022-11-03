using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankServer.BankDomain
{
    internal class BankStore
    {
        private float _balance; //only one client supported

        public BankStore()
        {
            _balance = 0;
        }

        public float Deposit(float amount) 
        {
            return _balance += amount;
        }

        public float Withdraw(float amount)
        {
            if (_balance >= amount)
                return _balance -= amount;
            else
                return 0;
        }
        public float ReadBalance()
        {
            return _balance;
        }
    }
}
