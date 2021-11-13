using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPF.App.Helpers;
using WPF.App.Interfaces;
using WPF.App.Public;

namespace WPF.App.Entities
{
    //Relatório
    public class Report : IReport
    {
        //Serviço de notificação
        private readonly INotifyService _notifyService;

        //Fila de execução de Clientes
        private List<Customer> _executionQueue;

        //Lista da Threads
        private List<Task> _sessionsThreads;

        //Lista dos tempos das Threads
        private List<int> _currentThreadsTime;

        private DateTime _start;

        //Horário atual global de todos os postos
        private int _currentGlobalTime;
       

        //Lista de registros
        public List<StepLog> Logs { get; set; }

        //Lista de sessões
        private List<Session> _sessions;

        //Lista de linhas de relatório
        public List<string> ReportLines { get; set; }

        //Porcentagem garantidade para clientes meia
        private const decimal HalfPricePercentage = 0.4M;



        //Construtor
        public Report(INotifyService notifyService)
        {
            //Definindo o serviço de notificação
            _notifyService = notifyService;

            //Inicializando variáveis
            _start = DateTime.Now;
            _currentGlobalTime = 1;
            _executionQueue = new List<Customer>();
            _sessionsThreads = new List<Task>();
            _currentThreadsTime = new List<int>();
            
            Logs = new List<StepLog>();
            ReportLines = new List<string>();
            
        

        }


        /// <summary>
        /// Executa o algoritmo do relatório
        /// </summary>
        /// <param name="sessions">Sessão com os clientes</param>
        public void Build(List<Session> sessions)
        {

            //Ordena as sessões por ordem de chegada e executa o algoritmo para clientes premium, meia entrada e normais
            _sessions = sessions.OrderBy(s => s.StartTime).ToList();

            //Para cada sessão cria uma thread para a execução em paralelo
            for (int i = 0; i < sessions.Count; i++)
            {
                //Como a execução é assíncrona é necessário garantir que o índice vai ser o correto
                int index = i;
                _sessionsThreads.Add(new Task(() => Execute(sessions[index])));
                _currentThreadsTime.Add(1);
                _sessionsThreads[index].Start();
            }

            Task.WaitAll(_sessionsThreads.ToArray());


            WriteThreadsResult();

        }

        /// <summary>
        /// Executa a session informada como parâmetro
        /// </summary>
        /// <param name="session">objeto da sessão completo</param>
        private void Execute(Session session)
        {
            PremiumCustomers(session);
            HalfPriceCustomers(session);
            RegularCustomers(session);
        }
        /// <summary>
        /// Gerar arquivo de relatório
        /// </summary>
        public async Task Generate()
        {
            //Criando arquivo
            var filePath = Util.GetFileToCreateFromExplorer();

            //Se o caminho do arquivo for nulo, sair do método
            if (filePath == null) return;
            try
            {

                await File.WriteAllLinesAsync(filePath, ReportLines);

                _notifyService.Alert(new Notification()
                {
                    Text = "Relatório salvo com sucesso!",
                    Type = AlertType.Success
                });
            }
            catch (Exception e)
            {
                _notifyService.Alert(new Notification()
                {
                    Text = "Não foi possível salvar o arquivo!",
                    Type = AlertType.Error
                });
            }
            
        }


        #region Execution
        /// <summary>
        /// Executa os clientes regulares
        /// </summary>
        /// <param name="session">Sessão de referência</param>
        private void RegularCustomers(Session session)
        {
            //Busca os clientes regulares
            var regularCustomers = session.Customers.Where(c => c.CustomerType == CustomerType.Regular).ToList();

            if (!regularCustomers.Any()) return;

            //Preenche a fila de execução com os clientes regulares
            FillExecutionQueue(regularCustomers);

            //Verifica para cada cliente se os assentos selecionados existem
            CheckIfSeatsExist(session);

            //Executa cada passo dos clientes
            ExecuteCustomersSteps(session, false);
        }
        /// <summary>
        /// Executa os clientes de meia entrada
        /// </summary>
        /// <param name="session">Sessão de referência</param>
        private void HalfPriceCustomers(Session session)
        {
            //Busca os clientes de meia entrada
            var halfPriceCustomers = session.Customers.Where(c => c.CustomerType == CustomerType.HalfPrice).ToList();

            if (!halfPriceCustomers.Any()) return;

            //Preenche a fila de execução com os clientes de meia entrada
            FillExecutionQueue(halfPriceCustomers);

            //Verifica para cada cliente se os assentos selecionados existem
            CheckIfSeatsExist(session);

            //Executa cada passo dos clientes
            ExecuteCustomersSteps(session, true);
        }

        /// <summary>
        /// Executa os clientes de meia entrada
        /// </summary>
        /// <param name="session">Sessão de referência</param>
        public void PremiumCustomers(Session session)
        {
            //Busca os clientes premium
            var premiumCustomers = session.Customers.Where(c => c.CustomerType == CustomerType.Premium).ToList();

            if (!premiumCustomers.Any()) return;

            //Preenche a fila de execução com os clientes premium
            FillExecutionQueue(premiumCustomers);

            //Verifica para cada cliente se os assentos selecionados existem
            CheckIfSeatsExist(session);

            //Executa cada passo dos clientes
            ExecuteCustomersSteps(session, false);


        }

        /// <summary>
        /// Executa os passos dos clientes
        /// </summary>
        /// <param name="session">sessão de referência</param>
        /// <param name="validateHalfPricePercentage">vailidar porcentagem garantidade de clientes de meia entrada</param>
        private void ExecuteCustomersSteps(Session session, bool validateHalfPricePercentage)
        {
            
            var seatsCount = session.Seats.Count;
            var sessionIndex = _sessions.IndexOf(session);
            bool moreThanMaximumHalfPrice;


            //Verifica se tem clientes executando que são da sessão
            while (HasCustomersExecutingInThisSession(session.StartTime))
            {
                //Porcentagem de assentos confirmados na sessão
                moreThanMaximumHalfPrice = (decimal)_sessions[sessionIndex].Seats.Count(c => !c.IsAvailable) / seatsCount > HalfPricePercentage;

                //Caso precise validar, estiver executando os clientes de meia entrada e estiverem acima de 40% modifica a fila de execução
                if (validateHalfPricePercentage && moreThanMaximumHalfPrice)
                    return;

                
                //Busca o próximo cliente da sessão pra execução 
                Customer nextCustomer = GetNextCustomer(session.StartTime);
               
                //Executa os passos do cliente na posição 1
                CheckSteps(nextCustomer, session);
            }



        }

        /// <summary>
        /// Executa os passos do cliente
        /// </summary>
        /// <param name="customer">Cliente</param>
        /// <param name="session">Sessão de referência</param>
        private void CheckSteps(Customer customer, Session session)
        {

            //Remove o cliente da fila de execução
            Monitor.Enter(_executionQueue);
            _executionQueue.Remove(customer);
            Monitor.Exit(_executionQueue);

            char[] steps;
            bool seatIsUnavailableAndCustomerGaveUp = false;

            steps = customer.Sequence.ToUpper().ToCharArray();

            foreach (var step in steps)
            {
                switch (step)
                {
                    //Na consulta verifica se o assento está disponível
                    case 'C':
                        //Caso o assento esteja indisponível e o cliente desistir para a execução dos passos
                        seatIsUnavailableAndCustomerGaveUp =  !CheckIfSeatIsAvailable(customer, session);
                        break;
                    //Seleciona o assento
                    case 'S':
                        ConfirmSeat(customer, session);
                        break;
                    //Paga o assento
                    case 'P':
                        Pay(customer, session);
                        break;
                    //Cancela a execução do cliente
                    case 'X':
                        CancelCustomer(customer, session);
                        break;

                }

                if (step == 'X' || seatIsUnavailableAndCustomerGaveUp)
                    break;
            }

            //Adiciona o log do cliente e calcula se ele está novamente na fila
            Logs.Add(new StepLog
            {
                Customer = customer,
                Session = session,
                Start = _currentGlobalTime,
                Finish = _currentGlobalTime + customer.EstimatedTime,
                TryCounter = GetTryCounterFromCustomer(customer),
                ThreadId = GetThreadId(session)

            });

            //Adiciona no tempo de execução o tempo do cliente
            _currentGlobalTime += customer.EstimatedTime;
            AddThreadTime(GetThreadId(session),customer.EstimatedTime);



        }

        /// <summary>
        /// Adiciona o tempo gasto na thread informada
        /// </summary>
        /// <param name="threadId">Índice da thread no array</param>
        /// <param name="customerTimeSpent">tempo gasto pelo cliente</param>
        private void AddThreadTime(int threadId, int customerTimeSpent)
        {
            _currentThreadsTime[threadId] += customerTimeSpent;
        }

        /// <summary>
        /// Pagamento do cliente, deixa a cadeira indisponível
        /// </summary>
        /// <param name="customer">Cliente</param>
        /// <param name="session">sessão de referência</param>
        private void Pay(Customer customer, Session session)
        {
            //Busca a cadeira do cliente e a sessão
            var seat = session.Seats.First(x => x.Identifier == customer.SelectedSeat);
            var seatIndex = session.Seats.IndexOf(seat);
            var sessionIndex = _sessions.IndexOf(session);

            //Muda o status na sessão e adiciona no relatório a informação
            seat.IsAvailable = false;
            seat.Customer = customer;
            seat.Color = Util.Red;

            _sessions[sessionIndex].Seats[seatIndex] = seat;
            ReportLines.Add($"Cliente {customer.ArrivalTime} Posto {GetThreadId(session)} {customer.SelectedSeat} {session.StartTime:HH:mm} confirmou.");
        }
        /// <summary>
        /// Cancela a execução do cliente
        /// </summary>
        /// <param name="customer">Cliente</param>
        /// <param name="session">sessão de referência</param>
        private void CancelCustomer(Customer customer, Session session)
        {
            //Adiciona a linha no relatório
            ReportLines.Add($"Cliente {customer.ArrivalTime} Posto {GetThreadId(session)} {customer.SelectedSeat} {session.StartTime:HH:mm} não confirmou.");

        }
        /// <summary>
        /// Seleciona o assento
        /// </summary>
        /// <param name="customer">Cliente</param>
        /// <param name="session">sessão de referência</param>
        private void ConfirmSeat(Customer customer, Session session)
        {


        }
        #endregion



        #region Util
        /// <summary>
        /// Verifica se o assento está disponível
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="session"></param>
        private bool CheckIfSeatIsAvailable(Customer customer, Session session)
        {
            //Verifica se o assento está disponível
            var isAvailable = session.Seats.First(Seat => Seat.Identifier == customer.SelectedSeat).IsAvailable;


            if (isAvailable)
                return true;


            //Se não tiver disponível e o cliente quiser tentar outro
            if (customer.OnUnavailableSeat == OnUnavailableSeatBehavior.TryAnother)
            {
                return TryAnotherSeat(customer, session);
                
            }
            //Adiciona no relatório que o cliente desistiu
            ReportLines.Add($"Cliente {customer.ArrivalTime} Posto {GetThreadId(session)} {customer.SelectedSeat} {session.StartTime:HH:mm} desistiu.");

            return false;
        }

        private bool TryAnotherSeat(Customer customer, Session session)
        {
            //Verifica a próxima cadeira disponível
            var availableSeat = GetNextAvailableSeat(session);

            //Se tiver cadeiras disponíveis ele seleciona essa cadeira para o cliente e insere o cliente novamente na fila pra executar
            if (availableSeat != null)
            {
                customer.SelectedSeat = availableSeat.Identifier;
                _executionQueue.Insert(0, customer);
            }
            return false;

        }

        /// <summary>
        /// Retorna a próxima cadeira disponível
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private Seat GetNextAvailableSeat(Session session)
        {
            return session.Seats.FirstOrDefault(c => c.IsAvailable);
        }
        private int GetTryCounterFromCustomer(Customer customer)
        {
            var tryCounter = 0;

            //Verifica as tentativas desse cliente 
            if (Logs.Exists(l => l.Customer.ArrivalTime == customer.ArrivalTime))
                tryCounter = Logs.Where(l => l.Customer == customer).Max(c => c.TryCounter) + 1;

            return tryCounter;

        }

        private void CheckIfSeatsExist(Session session)
        {
            Monitor.Enter(_executionQueue);
            var customersToRemove = new List<Customer>();
            foreach (var customer in _executionQueue)
            {
                if (!session.Seats.Any(Seat => Seat.Identifier == customer.SelectedSeat))
                    customersToRemove.Add(customer);
            }

            _executionQueue.RemoveAll(c => customersToRemove.Contains(c));
            Monitor.Exit(_executionQueue);
        }

        private void FillExecutionQueue(IEnumerable<Customer> customersToAdd)
        {
            Monitor.Enter(_executionQueue);
            _executionQueue.AddRange(customersToAdd);
            _executionQueue.OrderBy(c => c.ArrivalTime);
            Monitor.Exit(_executionQueue);
        }

        private Customer GetNextCustomer(DateTime sessionStartTime)
        {
            Monitor.Enter(_executionQueue);
            var nextCustomer = _executionQueue.First(c => c.SelectedSession == sessionStartTime);
            Monitor.Exit(_executionQueue);
            return nextCustomer;
        }

        private bool HasCustomersExecutingInThisSession(DateTime sessionStartTime)
        {
            Monitor.Enter(_executionQueue);
            bool hasCustomersExecutingInThisSession = _executionQueue.Any(c => c.SelectedSession == sessionStartTime);
            Monitor.Exit(_executionQueue);
            return hasCustomersExecutingInThisSession;
        }

        private int GetThreadId(Session session)
        {
            return _sessions.IndexOf(session);
        }

        private void WriteThreadsResult()
        {
            ReportLines.Add($"Horário de início: {_start:HH:mm:ss tt}");
            ReportLines.Add($"Horário de finalização:");

            for (int i = 0; i < _sessionsThreads.Count; i++)
            {
                ReportLines.Add($"Posto {i}: {_start.AddMinutes(_currentThreadsTime[i]):HH:mm:ss tt} - Minutos gastos: {_currentThreadsTime[i]}");
            }
        }
        #endregion

    }
}
