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
        private int _waitForNext;
        private int _lastTimeAdded;

        //Porcentagem garantidade para clientes meia
        private const decimal HalfPricePercentage = 0.4M;


        public Producer(List<Customer> customers, IExecution execution, bool useWaitingForNextCustomer)
        {
            _customers = customers;
            _execution = execution;
            _currentTime = 0;
            _waitForNext = 0;
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


        #endregion

        private void CanAddCustomersToQueue(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (ShouldAddToQueue())
            {
                _addCustomersToQueue.Stop();
                StartQueueMonitor();
                

                var customersToAdd = GetCustomers();

                AddCustomersToQueue(customersToAdd);
                EndQueueMonitor();
                _addCustomersToQueue.Start();
            }

            _currentTime++;
        }


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

        private bool ShouldAddToQueue()
        {
            if (_currentTime == 0)
                return true;


            return _lastTimeAdded + _waitForNext == _currentTime;


        }
        private void AddCustomersToQueue(List<Customer> customerToAdd)
        {

            if (customerToAdd.Count == 0)
                return;


            foreach (var customer in customerToAdd)
            {
                if (ShouldRearrangeQueue(customer))
                {
                    RearrangeAndAddToQueue(customer);
                    continue;

                }

                _execution.ExecutionQueue.Add(customer);

            }

            _waitForNext = customerToAdd.Last().TimeWaitForNext;

            _lastTimeAdded = _currentTime;
        }

        private void RearrangeAndAddToQueue(Customer customer)
        {
            List<Customer> newExecutionQueue = new List<Customer>();

            var premium = _execution.ExecutionQueue.Where(c=>c.CustomerType==CustomerType.Premium).OrderByDescending(c=>c.ArrivalTime).ToList();

            if(customer.CustomerType== CustomerType.Premium)
                premium.Add(customer);


            var halfPrice = _execution.ExecutionQueue.Where(c=>c.CustomerType==CustomerType.HalfPrice).OrderByDescending(c => c.ArrivalTime).ToList();

            if (customer.CustomerType == CustomerType.HalfPrice)
                halfPrice.Add(customer);


            var regular = _execution.ExecutionQueue.Where(c=>c.CustomerType==CustomerType.Regular).OrderByDescending(c => c.ArrivalTime).ToList();

            if (customer.CustomerType == CustomerType.Regular)
                regular.Add(customer);


            newExecutionQueue.AddRange(premium);
            newExecutionQueue.AddRange(halfPrice);
            newExecutionQueue.AddRange(regular);


            _execution.ExecutionQueue = newExecutionQueue;

        }

        private bool ShouldRearrangeQueue(Customer customer)
        {
            var customerType = customer.CustomerType;

            var seatsCount = _execution.Sessions[GetCustomerSessionIndex(customer)].Seats.Count;

            var moreThanMaximumHalfPrice = (decimal)_execution.Sessions[GetCustomerSessionIndex(customer)].Seats.Count(s => s.Status==Status.Unavailable ) / seatsCount > HalfPricePercentage;

            var isHalfPriceAndValid = customerType == CustomerType.HalfPrice && !moreThanMaximumHalfPrice;

            if (HasCustomersUnder(customerType)&&isHalfPriceAndValid)
                return true;


            return false;

        }

        private bool HasCustomersUnder(CustomerType customerType)
        {
            return _execution.ExecutionQueue.Any(c => (int)c.CustomerType < (int)customerType);
        }

        private List<Customer> GetCustomers()
        {
            List<Customer> customersToAdd = new List<Customer>();
            Customer currentCustomer;
            while ((currentCustomer = GetNextCustomer())!=null && currentCustomer.TimeWaitForNext == 0)
            {
                customersToAdd.Add(currentCustomer);
            }

            return customersToAdd;
        }


        private Customer GetNextCustomer()
        {
            Customer nextCustomer = null;
            var customerIndex = _customers.FindIndex(c => c.ArrivalTime == _currentTime);

            if (customerIndex != -1)
            {
                nextCustomer = _customers[customerIndex];
                _customers.RemoveAt(customerIndex);
            }

            return nextCustomer;
        }
        public int GetCustomerSessionIndex(Customer customer)
        {
            var customerSessionIndex =
                _execution.Sessions.FindIndex(s => s.StartTime == customer.SelectedSession);
            return customerSessionIndex;
        }
        #endregion


        public void StartQueueMonitor() => Monitor.Enter(_execution.ExecutionQueue);
        public void EndQueueMonitor() => Monitor.Exit(_execution.ExecutionQueue);
    }
}
