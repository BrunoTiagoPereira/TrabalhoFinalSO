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
        #region Props

        //Singleton de execução
        private readonly IExecution _execution;
        //Timer pra adiciona o cliente na fila
        private Timer _addCustomersToQueue;
        //Parâmetro pra esperar o tempo do próximo cliente (interface)
        private readonly bool _shouldWaitForNextCustomer;
        //Tempo atual do produtor
        private int _currentTime;
        //Tempo até o próximo cliente
        private int _waitForNext;
        //Ultimo tempo que foi adicionado um cliente
        private int _lastTimeAdded;
        //Clientes
        public List<Customer> Customers { get; set; }
        //Clientes que estão tentando novos assentos pois estavam indisponíveis
        public List<Customer> _customersToTryAnotherSeat;

        //Bool se está executando
        public bool IsExecuting { get; set; }

        //Porcentagem garantidade para clientes meia
        private const decimal HalfPricePercentage = 0.4M;

        #endregion

        public Producer(List<Customer> customers, IExecution execution, bool shouldWaitForNextCustomer)
        {
         
            Customers = customers;
            _execution = execution;
            _currentTime = 1;
            _waitForNext = 0;
            _lastTimeAdded = 1;
            _shouldWaitForNextCustomer = shouldWaitForNextCustomer;

            FilterAvailableSeats();
            InstanceTimerForAddingToQueue();

            _customersToTryAnotherSeat = new List<Customer>();
            _execution.TryAnotherSeat += TryAnotherSeat;

        }

        #region Instances
        private void InstanceTimerForAddingToQueue()
        {
            IsExecuting = true;
            _addCustomersToQueue = new Timer();
            _addCustomersToQueue.Enabled = true;
            _addCustomersToQueue.Interval = 1000;
            _addCustomersToQueue.Elapsed += CanAddCustomersToQueue;
            _addCustomersToQueue.Start();
            

        }


        #endregion

        #region Timers

        /// <summary>
        /// Adiciona os clientes na fila
        /// </summary>
        private void CanAddCustomersToQueue(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Se não tiver clientes termina
            if (Customers.Count == 0)
            {
                Finish();
                return;
            }

            _addCustomersToQueue.Stop();

            StartQueueMonitor();
            StartAllSessionsMonitor();

            //Adiciona os clientes que estão tentando outra cadeira
            AddCustomersToQueue(_customersToTryAnotherSeat);

            //Para os clientes normais verifica se deve adicionar na fila no tempo  atual
            if (ShouldAddToQueue())
            {
                //Busca os clientes do tempo atual
                var customersToAdd = GetCustomers();
                //Adiciona os clientes
                AddCustomersToQueue(customersToAdd);
            }

            EndAllSessionsMonitor();
            EndQueueMonitor();

            _addCustomersToQueue.Start();

            _currentTime++;

            if (_waitForNext != 0)
                _waitForNext--;
        }

        #endregion

        #region Execution
        /// <summary>
        /// Verifica se pode ser adiciona algum cliente na fila
        /// </summary>
        
        private bool ShouldAddToQueue()
        {
            return _waitForNext == 0;
        }

        /// <summary>
        /// Adiciona os clientes na fila
        /// </summary>
        /// <param name="customerToAdd">Clientes</param>
        private void AddCustomersToQueue(List<Customer> customerToAdd)
        {
            //Caso a lista não tiver elementos retorna
            if (customerToAdd.Count == 0)
                return;

            //Para cada cliente verifica, se precisar remontar a fila, remonta e adiciona e, caso não, só adiciona
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
            }
            //Adiciona na lista os clientes finalizados
            _execution.ProducerFinished.AddRange(customerToAdd);

            
            _lastTimeAdded = _currentTime;
        }
        /// <summary>
        /// Remonta a fila e adiciona o cliente nela conforme as prioridades
        /// </summary>
        /// <param name="customer">Cliente para adicionar</param>
        private void RearrangeAndAddToQueue(Customer customer)
        {
            List<Customer> newExecutionQueue = new List<Customer>();

            //Busca os clientes premium ordenados por tempo de chegada
            var premium = _execution.ExecutionQueue.Where(c => c.CustomerType == CustomerType.Premium).OrderBy(c => c.ArrivalTime).ToList();

            //Caso o cliente para adicionar for premium adiciona ele no final
            if (customer.CustomerType == CustomerType.Premium)
                premium.Add(customer);

            //organiza o resto (caso o cliente fazer parte desse resto já adiciona ele na fila)
            var rest = RearrangeRest(customer);

            //Adiciona os clientes premium, o resto e cria a nova fila
            newExecutionQueue.AddRange(premium);
            newExecutionQueue.AddRange(rest);

            _execution.ExecutionQueue.Clear();
            _execution.ExecutionQueue.AddRange(newExecutionQueue);


        }

        /// <summary>
        /// Remonta o resto da fila que não é premium
        /// </summary>
        /// <param name="customer">Cliente para adicionar, caso precise</param>
        private List<Customer> RearrangeRest(Customer customer)
        {

            List<Customer> customers = new List<Customer>();
            List<Customer> rest = new List<Customer>();

            //Tipo do cliente
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
            customers.AddRange(rest.OrderBy(c => c.ArrivalTime));


            return customers;


        }
        /// <summary>
        /// Verifica se precisa remontar a fila baseado no cliente passado como parâmetro
        /// </summary>
        /// <param name="customer">Cliente de referência</param>
        /// <returns>True se precisa</returns>
        private bool ShouldRearrangeQueue(Customer customer)
        {
            //Tipo do cliente
            var customerType = customer.CustomerType;

            //Quantidade de assentos na sessão do cliente
            var seatsCount = _execution.Sessions[GetCustomerSessionIndex(customer)].Seats.Count;

            StartSessionMonitor(customer);
            //Verifica se a quantidade de assentos indisponívies é maior que o máximo de meia entrada
            var moreThanMaximumHalfPrice = (decimal)_execution.Sessions[GetCustomerSessionIndex(customer)].Seats.Count(s => s.Status == Status.Unavailable) / seatsCount > HalfPricePercentage;
            EndSessionMonitor(customer);

            //Verifica se o cliente é meia entrada e ainda tem prioridade
            var isHalfPriceAndValid = customerType == CustomerType.HalfPrice && !moreThanMaximumHalfPrice;

            //Verifica se há clientes em uma hierarquia menor na fila
            var hasCustomersUnder = HasCustomersUnder(customerType);
            //Retorna true se:
            //Caso tenha clientes menores na hierarquia, o cliente for meia entrada e ainda tiver prioridade
            //Caso tenha clientes menores na hierarquia e o cliente não for meia entrada
            if ((hasCustomersUnder && isHalfPriceAndValid) || (hasCustomersUnder && customerType != CustomerType.HalfPrice))
                return true;


            return false;

        }
        /// <summary>
        /// Verifica se há algum cliente abaixo na hierarquia que o tipo passado como parâmetro
        /// </summary>
        /// <param name="customerType">Tipo do cliente</param>
        /// <returns>True se há</returns>
        private bool HasCustomersUnder(CustomerType customerType)
        {
            return _execution.ExecutionQueue.Any(c => (int)c.CustomerType < (int)customerType);
        }

        /// <summary>
        /// Adiciona os clientes que precisam de outros assentos na fila
        /// </summary>
        private void TryAnotherSeat(object sender, Customer e)
        {
            _customersToTryAnotherSeat.Add(e);
        }
        #endregion

        #region Util


        /// <summary>
        /// Remove todos os clientes que os assentos não existem
        /// </summary>
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

      
    
   
        /// <summary>
        /// Verifica o limite de meia entrada da sessão, considerando caso esteja sendo adicionado no momento
        /// </summary>
        /// <param name="customerSession">Sessão do cliente</param>
        /// <param name="toAddInSameSession">Quantidade de clientes que estão sendo adicionados no momento</param>
        /// <returns></returns>
        private int GetSessionLimitCounter(Session customerSession, int toAddInSameSession)
        {
            //Premium que pagaram os assentos
            var premiumOnPaidSeat = customerSession.Seats
                .Where(s => s.Status == Status.Unavailable)
                .Count(s => s.Customer.CustomerType == CustomerType.Premium);

            //Premium que estão na fila de execução
            var premiumOnExecutionQueue = _execution.ExecutionQueue
                .Count(c => c.CustomerType == CustomerType.Premium);

            return premiumOnPaidSeat + premiumOnExecutionQueue + toAddInSameSession;
        }


        /// <summary>
        /// Busca os próximos clientes pra adicionar
        /// </summary>
        private List<Customer> GetCustomers()
        {
            List<Customer> customersToAdd = new List<Customer>();
            Customer currentCustomer;
            //Enquanto o próximo cliente não for nulo adiciona ele na fila, caso passado como parâmetro espera o tempo do próximo cliente (interface)
            while ((currentCustomer = GetNextCustomer())!=null)
            {
                customersToAdd.Add(currentCustomer);

                if (_shouldWaitForNextCustomer)
                    _waitForNext = currentCustomer.TimeWaitForNext;

            }

            return customersToAdd;
        }

        /// <summary>
        /// Busca o próximo cliente
        /// </summary>
        /// <returns></returns>
        private Customer GetNextCustomer()
        {
            //Se não houver clientes ou precisar esperar mais tempo retorna
            if (Customers.Count == 0 || _waitForNext != 0)
                return null;

            //Busca o próximo cliente na fila
            int customerIndex = 0;
            var nextCustomer = Customers[customerIndex];
            Customers.RemoveAt(customerIndex);

            return nextCustomer;
        }
        //Busca o índice da sessão do cliente
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

        #region Monitors
        private void StartQueueMonitor() => Monitor.Enter(_execution.ExecutionQueue);
        private void EndQueueMonitor() => Monitor.Exit(_execution.ExecutionQueue);

        private void StartSessionMonitor(Customer customer) => Monitor.Enter(_execution.Sessions[GetCustomerSessionIndex(customer)]);
        private void EndSessionMonitor(Customer customer) => Monitor.Exit(_execution.Sessions[GetCustomerSessionIndex(customer)]);

        private void StartAllSessionsMonitor() => Monitor.Enter(_execution.Sessions);
        private void EndAllSessionsMonitor() => Monitor.Exit(_execution.Sessions);
        #endregion

    }
}
