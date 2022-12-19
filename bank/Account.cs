using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bank
{
    public class Account
    {
        //
        // var
        //
        public uint bank { get; }
        public uint id { get; }
        private uint balance { get; set; }

        // lock object (if necessary)
        //private object obj = new object();

        //
        // constructor
        //
        public Account(uint b, uint i) {
            bank = b;
            id = i;
            balance = 3000;
        }

        //
        // memeber function
        //
        public uint account_amount
        {
            get { return balance; }
        }

        // note: return inside lock is accetable
        //https://stackoverflow.com/questions/266681/should-a-return-statement-be-inside-or-outside-a-lock
        public bool deposit(uint num) {
            lock ((object)balance)
            {
                balance += num;
                return true;
            }
        }
        public bool withdraw(uint num) {
            lock ((object)balance)
            {
                if (balance < num)
                {
                    return false;
                }
                else
                {
                    balance -= num;
                    return true;
                }
            }
        }
        
    }
}
