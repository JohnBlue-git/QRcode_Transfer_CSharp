using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//
// for C# thread and C# container
//
using System.Threading;
using System.Collections;
//using System.Collections.Generic;

namespace bank
{
    public class Central
    {
        //
        // Member var
        //
        private Hashtable accounts;// <uint, Account>
        private List<Transfer_Info> history_transfer;
        public ICollection source_account { get { return accounts.Values; } }
        public ICollection source_history { get { return history_transfer; } }

        //
        // Lock
        //
        private int set_max;// for setting thread pool size
        private static Semaphore semp;

        //
        // Information container
        //
        private class Status
        {
            public CountdownEvent key;// countdown lock
            public string state;
            public Transfer_Info info;
        }

        //
        // Constructor
        //
        public Central()
        {
            // initializing memeber var
            accounts = new Hashtable();
            history_transfer = new List<Transfer_Info>();

            // initializing semaphore and thread pool
            set_max = 30;
            semp = new Semaphore(0, set_max);
            semp.Release(set_max);
            ThreadPool.SetMaxThreads(set_max, set_max);
        }

        //
        // Member func
        //
        private bool valid_account(uint serial)
        {
            // serial = id(000) + bank id(000)
            if (serial >= 1000000) return false;

            // bank id: 0 ~ 38
            uint bk = serial % 1000;
            if (bk < 0 || bk > 38) return false;

            return true;
        }
        public bool inquire_account(uint serial)
        {
            return valid_account(serial) && accounts.ContainsKey(serial);
        }
        public Account update_info(uint serial)
        {
            if (inquire_account(serial)) return (Account)accounts[serial];
            return null;
        }

        private void create_account(object ms)
        {
            // ms turning to information
            Status sat = (Status)ms;
            CountdownEvent key = sat.key;
            ref string state = ref sat.state;
            Transfer_Info info = (Transfer_Info)sat.info;

            // valid checking
            if (!valid_account(info.serial_A))
            {
                state = "Invalid account";

                // returning
                key.Signal();// coundown lock signal
                return;
            }
            if (accounts.ContainsKey(info.serial_A))
            {
                state = "Existed account";

                // returning
                key.Signal();// coundown lock signal
                return;
            }

            // creating account
            Account acc = new Account(info.serial_A % 1000, info.serial_A / 1000);
            // critical section start
            lock (accounts)
            {
                accounts.Add(info.serial_A, acc);
            }
            // critical section end
            state = "Success create";

            // returning
            key.Signal();// coundown lock signal
            // return
        }
        private void delete_account(object ms)
        {
            // ms turing to information
            Status sat = (Status)ms;
            CountdownEvent key = sat.key;
            ref string state = ref sat.state;
            Transfer_Info info = (Transfer_Info)sat.info;

            // vaild checking
            if (!valid_account(info.serial_A))
            {
                state = "Invalid account";

                // retuning
                key.Signal();// coundown lock signal
                return;
            }
            if (!accounts.ContainsKey(info.serial_A))
            {
                state = "Inexisted account";

                // return
                key.Signal();// coundown lock signal
                return;
            }

            // deleting account
            // critical section start
            lock (accounts)
            {
                accounts.Remove(info.serial_A);
            }
            // critical section end
            state = "Success delete";

            // returning
            key.Signal();// coundown lock signal
            // return
        }
        private void request_transfer(object ms)
        {
            // ms turing to information
            Status sat = (Status)ms;
            CountdownEvent key = sat.key;
            ref string state = ref sat.state;
            Transfer_Info info = (Transfer_Info)sat.info;

            // valid checking
            if (!valid_account(info.serial_A) || !valid_account(info.serial_B))
            {
                state = "Invalid account";

                // coundown lock signal
                key.Signal();
                return;
            }
            if (!accounts.ContainsKey(info.serial_A) || !accounts.ContainsKey(info.serial_B))
            {
                state = "Inexisted account";

                // returning
                key.Signal();// coundown lock signal
                return;
            }

            // A transfering to B
            Account A = (Account)accounts[info.serial_A];
            Account B = (Account)accounts[info.serial_B];
            if (A.account_amount < info.amount)
            {
                state = "Amount not enough";

                // returning
                key.Signal();// coundown lock signal
                return;
            }
            A.withdraw(info.amount);// has critcal section inside object
            B.deposit(info.amount);// has critical section inside
            // critical section start
            lock (history_transfer)
            {
                history_transfer.Add(info);
            }
            // critical section end
            state = "Success transfer";

            // returning
            key.Signal();// coundown lock signal
            //return;
        }

        public string remote_request(string req, Transfer_Info enter)
        {
            // requesting
            if (semp.WaitOne(1000))// semaphore wait for one second
            {
                //Thread.Sleep(1000);

                // trying to assigning task to threadpool
                Status ms = new Status();// object for contain information
                try
                {
                    // crating new countdown lock
                    ms.key = new CountdownEvent(1);
                    
                    // loading information
                    ms.state = "null";
                    ms.info = enter;

                    // calling threadpool
                    if (req == "create") ThreadPool.QueueUserWorkItem(create_account, (object)ms);
                    else if (req == "delete") ThreadPool.QueueUserWorkItem(delete_account, (object)ms);
                    else if (req == "transfer") ThreadPool.QueueUserWorkItem(request_transfer, (object)ms);
                    else return "Requested service not exists";

                    // countdown lock (wait till the task is signaled)
                    ms.key.Wait();
                    //not workable if (!ms.key.Wait(1000)) ms.key.Signal();
                    // when threadpool is assigned task
                    // lock.wait()
                    //  when task end
                    // lock.signal()
                }
                catch (Exception)
                {
                    ms.state = "Unkonwn error";
                }
                finally
                {
                    //semp.Release();
                }

                // returning
                semp.Release();
                return ms.state;
            }
            else
            {
                // returning
                semp.Release();
                return "Currently out of services";
            }
        }
    }
}
