using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPF.App.Interfaces;
using Timer = System.Timers.Timer;

namespace WPF.App.Entities
{
    public class Producer
    {
        private readonly List<Customer> _customers;
        private readonly IExecution _execution;
        private readonly bool _useWaitingForNextCustomer;
        private Timer _addCustomersToQueue;
        private int _currentTime;
        private int _lastTimeAdded;

        public Producer(List<Customer> customers, IExecution execution, bool useWaitingForNextCustomer)
        {
            _customers = customers;
            _execution = execution;
            _currentTime = 0;
            _lastTimeAdded = 0;
            _useWaitingForNextCustomer = useWaitingForNextCustomer;

            FilterAvailableSeats();
            InstanceTimerForAddingToQueue();
            
        }
        #region Instances
        private void InstanceTimerForAddingToQueue()
        {
            _addCustomersToQueue.Enabled = true;
            _addCustomersToQueue.Interval = 1000;
            _addCustomersToQueue.Elapsed += CanAddCustomersToQueue;
            _addCustomersToQueue.Start();

        }

        private void CanAddCustomersToQueue(object sender, System.Timers.ElapsedEventArgs e)
        {
            //if()
        }
        #endregion

        #region Util
        private void FilterAvailableSeats()
        {


            var customersToRemove = new List<Customer>();
            foreach (var customer in _customers)
            {
                var session = _execution.Sessions.Find(s => s.StartTime == customer.SelectedSession);
                if (!session.Seats.Any(s => s.Identifier == customer.SelectedSeat))
                    customersToRemove.Add(customer);
            }

            _customers.RemoveAll(c => customersToRemove.Contains(c));

        }
        #endregion


        public void StartQueueMonitor() => Monitor.Enter(_execution.ExecutionQueue);
        public void EndQueueMonitor() => Monitor.Exit(_execution.ExecutionQueue);
    }
}
