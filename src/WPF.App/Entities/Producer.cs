using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using WPF.App.Interfaces;
using Timer = System.Timers.Timer;


namespace WPF.App.Entities
{
    public class Producer
    {
        
        private readonly IExecution _execution;
        private readonly bool _useWaitingForNextCustomer;
        private Timer _addCustomersToQueue;
        private int _currentTime;
        private int _waitForNext;
        private int _lastTimeAdded;
        public List<Customer> Customers { get; set; }
        public bool IsExecuting { get; set; }

        //Porcentagem garantidade para clientes meia
        private const decimal HalfPricePercentage = 0.4M;


        public Producer(List<Customer> customers, IExecution execution, bool useWaitingForNextCustomer)
        {
            IsExecuting = true;
            Customers = customers;
            _execution = execution;
            _currentTime = 1;
            _waitForNext = 0;
            _lastTimeAdded = 1;
            _useWaitingForNextCustomer = useWaitingForNextCustomer;

            FilterAvailableSeats();
            InstanceTimerForAddingToQueue();
            
        }
        #region Instances
        private void InstanceTimerForAddingToQueue()
        {
            _addCustomersToQueue = new Timer();
            _addCustomersToQueue.Enabled = true;
            _addCustomersToQueue.Interval = 1000;
            _addCustomersToQueue.Elapsed += CanAddCustomersToQueue;
            _addCustomersToQueue.Start();
            

        }


        #endregion

        #region Timers

        private void CanAddCustomersToQueue(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Customers.Count == 0)
            {
                Finish();
                return;
            }

            _addCustomersToQueue.Stop();
               


            if (ShouldAddToQueue())
            {

                StartQueueMonitor();
                StartAllSessionsMonitor();
                var customersToAdd = GetCustomers();
                AddCustomersToQueue(customersToAdd);
                EndAllSessionsMonitor();
                EndQueueMonitor();


            }

            _addCustomersToQueue.Start();

            _currentTime++;

            if (_waitForNext != 0)
                _waitForNext--;
        }

        #endregion

        #region Util

     
        private void FilterAvailableSeats()
        {


            var customersToRemove = new List<Customer>();
            foreach (var customer in Customers)
            {
                var session = _execution.Sessions.Find(s => s.StartTime == customer.SelectedSession);
                if (!session.Seats.Any(s => s.Identifier == customer.SelectedSeat))
                    customersToRemove.Add(customer);
            }

            Customers.RemoveAll(c => customersToRemove.Contains(c));

        }

        private bool ShouldAddToQueue()
        {
            return _waitForNext==0;
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
                }
                else
                {
                    _execution.ExecutionQueue.Add(customer);
                }


                if (_useWaitingForNextCustomer)
                {
                    Thread.Sleep(customer.EstimatedTime*1000);
                }
            
                

            }

            _execution.ProducerFinished.AddRange(customerToAdd);

            _lastTimeAdded = _currentTime;
        }

        private void RearrangeAndAddToQueue(Customer customer)
        {
            List<Customer> newExecutionQueue = new List<Customer>();

            var premium = _execution.ExecutionQueue.Where(c=>c.CustomerType==CustomerType.Premium).OrderBy(c=>c.ArrivalTime).ToList();

            if(customer.CustomerType== CustomerType.Premium)
                premium.Add(customer);

            var rest = RearrangeRest(customer);

            newExecutionQueue.AddRange(premium);
            newExecutionQueue.AddRange(rest);

            _execution.ExecutionQueue.Clear();
            _execution.ExecutionQueue.AddRange(newExecutionQueue);


        }

        private List<Customer> RearrangeRest(Customer customer)
        {

            List<Customer> customers = new List<Customer>();
            List<Customer> rest = new List<Customer>();

            var customerType = customer.CustomerType;

            //Busca os clientes de meia entrada ordenados
            var halfPriceCustomers = _execution.ExecutionQueue
                .Where(c => c.CustomerType == CustomerType.HalfPrice)
                .OrderBy(c => c.ArrivalTime)
                .ToList();

            //Busca os clientes regulares ordenados
            var regularCustomers = _execution.ExecutionQueue
                .Where(c => c.CustomerType == CustomerType.Regular)
                .OrderBy(c => c.ArrivalTime)
                .ToList();
           
            //Array que considera os clientes que estão sendo adicionados no momento
            var toAddInSession = new int[_execution.Sessions.Count];

            //Adiciona o cliente na lista respectiva
            if (customerType == CustomerType.HalfPrice)
                halfPriceCustomers.Add(customer);
            else if (customerType == CustomerType.Regular)
                regularCustomers.Add(customer);

            //Adiciona os clientes regulares no faltante
            rest.AddRange(regularCustomers);
            

            //Para cada cliente de meia entrada verifica se na sessão ele tem prioridade e caso sim, adiciona ele na lista principal e, caso não, adiciona ele na de faltantes
            foreach (var c in halfPriceCustomers)
            {
                var customerSessionIndex = GetCustomerSessionIndex(customer);
                var customerSession = _execution.Sessions[customerSessionIndex];
                var sessionLimitCount = GetSessionLimitCounter(customerSession, toAddInSession[customerSessionIndex]);
                var maxHalfPriceCustomersCount = (int)(HalfPricePercentage * customerSession.Seats.Count) - sessionLimitCount;

                if (maxHalfPriceCustomersCount != 0)
                    customers.Add(c);
                else
                    rest.Add(c);

                toAddInSession[customerSessionIndex]++;
            }

            //adiciona a lista de faltantes ordenadas na principal 
            customers.AddRange(rest.OrderBy(c=>c.ArrivalTime));


            return customers;


        }

        private int GetSessionLimitCounter(Session customerSession, int toAddInSameSession)
        {
            var premiumOnPaidSeat = customerSession.Seats
                .Where(s => s.Status == Status.Unavailable)
                .Count(s => s.Customer.CustomerType == CustomerType.Premium);

            var premiumOnExecutionQueue = _execution.ExecutionQueue
                .Count(c => c.CustomerType == CustomerType.Premium);

            return premiumOnPaidSeat + premiumOnExecutionQueue + toAddInSameSession;
        }

        private bool ShouldRearrangeQueue(Customer customer)
        {
            var customerType = customer.CustomerType;

            var seatsCount = _execution.Sessions[GetCustomerSessionIndex(customer)].Seats.Count;

            StartSessionMonitor(customer);
            var moreThanMaximumHalfPrice = (decimal)_execution.Sessions[GetCustomerSessionIndex(customer)].Seats.Count(s => s.Status==Status.Unavailable ) / seatsCount > HalfPricePercentage;
            EndSessionMonitor(customer);
            var isHalfPriceAndValid = customerType == CustomerType.HalfPrice && !moreThanMaximumHalfPrice;

            var hasCustomersUnder = HasCustomersUnder(customerType);
            if ((hasCustomersUnder&&isHalfPriceAndValid)||(hasCustomersUnder&&customerType!=CustomerType.HalfPrice))
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
            while ((currentCustomer = GetNextCustomer())!=null)
            {
                customersToAdd.Add(currentCustomer);
                _waitForNext = currentCustomer.TimeWaitForNext;
            }

            return customersToAdd;
        }


        private Customer GetNextCustomer()
        {
            if (Customers.Count == 0 || _waitForNext != 0)
                return null;

            int customerIndex = 0;
            var nextCustomer = Customers[customerIndex];
            Customers.RemoveAt(customerIndex);

            return nextCustomer;
        }
        public int GetCustomerSessionIndex(Customer customer)
        {
            var customerSessionIndex =
                _execution.Sessions.FindIndex(s => s.StartTime == customer.SelectedSession);
            return customerSessionIndex;
        }

        private void Finish()
        {
            IsExecuting = false;
            _addCustomersToQueue.Stop();

        }
        #endregion


        private void StartQueueMonitor() => Monitor.Enter(_execution.ExecutionQueue);
        private void EndQueueMonitor() => Monitor.Exit(_execution.ExecutionQueue);

        private void StartSessionMonitor(Customer customer) => Monitor.Enter(_execution.Sessions[GetCustomerSessionIndex(customer)]);
        private void EndSessionMonitor(Customer customer) => Monitor.Exit(_execution.Sessions[GetCustomerSessionIndex(customer)]);

        private void StartAllSessionsMonitor() => Monitor.Enter(_execution.Sessions);
        private void EndAllSessionsMonitor() => Monitor.Exit(_execution.Sessions);
    }
}
