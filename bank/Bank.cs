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
    public class Bank
    {
        //
        // Member var
        //
        private Central central;// central bank
        private string name;// name of bank
        private uint bank_code;// code of bank
        public string get_name { get { return name; } }
        public uint get_bank { get { return bank_code; } }

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
        public Bank(Central ct, string nm, uint bc)
        {
            // initializing var
            central = ct;
            name = nm;
            bank_code = bc;

            // initializing lock
            set_max = 3;
            semp = new Semaphore(0, set_max);
            semp.Release(set_max);
            ThreadPool.SetMaxThreads(set_max, set_max);
        }

        //
        // Memeber func
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
        public bool inquire_account(uint serial)// existed?
        {
            return central.inquire_account(serial);
        }
        public Account update_info(uint serial)// account info
        {
            if (inquire_account(serial)) return (Account)central.update_info(serial);
            return null;
        }

        private void create_account(object ms)
        {
            // ms turing to information
            Status sat = (Status)ms;
            CountdownEvent key = sat.key;
            ref string state = ref sat.state;
            Transfer_Info info = (Transfer_Info)sat.info;

            // valid checking
            if (!valid_account(info.serial_A))
            {
                state = "Invalid account";

                // returning
                key.Signal();
                return;
            }
            if (central.inquire_account(info.serial_A))
            {
                state = "Existed account";

                // returing
                key.Signal();
                return;
            }
            
            // calling to central bank and creating account
            state = central.remote_request("create", info);

            // returning
            key.Signal();
            //return;
        }
        private void delete_account(object ms)
        {
            // ms turing to information
            Status sat = (Status)ms;
            CountdownEvent key = sat.key;
            ref string state = ref sat.state;
            Transfer_Info info = (Transfer_Info)sat.info;

            // valid checking
            if (!valid_account(info.serial_A))
            {
                state = "Invalid account";

                // returning
                key.Signal();
                return;
            }
            if (!central.inquire_account(info.serial_A))
            {
                state = "Inexisted account";

                // returning
                key.Signal();
                return;
            }

            // calling to central bank and deleting account
            state = central.remote_request("delete", info);

            // returning
            key.Signal();
            //return;
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

                // retuning
                key.Signal();
                return;
            }
            if (!central.inquire_account(info.serial_A) || !central.inquire_account(info.serial_B))
            {
                state = "Inexisted account";

                // returning
                key.Signal();
                return;
            }

            // calling to central bank for transfering
            state = central.remote_request("transfer", info);

            // returning
            key.Signal();
            //return;
        }

        public string remote_request(string req, Transfer_Info enter)
        {
            // requesting
            if (semp.WaitOne(1000))// semaphore wait for one second
            {
                //Thread.Sleep(1000);

                // trying to assigning task to threadpool
                Status ms = new Status();// object for containing information
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
                    // not workable if (!ms.key.Wait(1000)) ms.key.Signal();
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

                // retuning
                semp.Release();
                return ms.state;
            }
            else
            {
                // retuning
                semp.Release();
                return "Currently out of services";
            }
        }
    }
}
