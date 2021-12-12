using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPF.App.Helpers;
using WPF.App.Interfaces;
using WPF.App.Public;
using Timer = System.Timers.Timer;

namespace WPF.App.Entities
{

    /// <summary>
    /// Consumidor
    /// </summary>
    public class Consumer
    {

        #region Props
        //Singleton de execução
        private readonly IExecution _execution;

        //Id do posto
        public int Id { get; set; }

        //Intervalo padrão
        private const int TimerInterval = 1000;

        //Thread atual que está sendo executado
        public Task CurrentThread { get; set; }

        //Bool se está executando
        public bool IsExecuting { get; private set; }


        //Timer pra verificar a fila
        private Timer _checkQueue;

        //Timer para verificar cadeira pendente
        private Timer _waitForPending;

        //Esperar o tempo do cliente ou não (interface)
        private bool _shouldWaitCustomerTime;


        //Propriedades atuais do consumidor para fácil acesso
        private Customer _currentCustomer;
        private int _customerSessionIndex;
        private int _customerSeatIndex;
        private Session _customerSession;
        private Seat _selectedSeat;
        #endregion

        #region Events

        //Evento de novo steplog
        public static event EventHandler<StepLog> AddStepLog;
        //Evento de adicionar na linha do relatório
        public static event EventHandler<string> AddReportLog;
        //Quando aumenta o tempo total da aplicação
        public static event EventHandler OnCurrentGlobalTimeChanged; 
        #endregion
        public Consumer(IExecution execution, int id, bool shouldWaitCustomerTime)
        {
            Id = id;
            _shouldWaitCustomerTime = shouldWaitCustomerTime;
            _execution = execution;

            InstanceTimerForCheckingQueue();
            InstanceTimerForWaitingForPending();




        }

        #region Instances
        private void InstanceTimerForCheckingQueue()
        {
            _checkQueue = new Timer();
            _checkQueue.Enabled = true;
            _checkQueue.Interval = TimerInterval;
            _checkQueue.Elapsed += CheckQueue;
            _checkQueue.Start();
            
        }

        private void InstanceTimerForWaitingForPending()
        {
            _waitForPending = new Timer();
            _waitForPending.Enabled = false;
            _waitForPending.Interval = TimerInterval;
            _waitForPending.Elapsed += WaitForPending;

        }
        #endregion

        #region TimersCheck
        /// <summary>
        /// Verifica se o assento saiu do status de pendente
        /// </summary>
        private void WaitForPending(object sender, System.Timers.ElapsedEventArgs e)
        {
            CanConsumeSeat();

        }

        /// <summary>
        /// Verifica se há elementos na fila pra execução e caso o consumidor não esteja executando, cria uma thread e executa com o próximo elemento 
        /// </summary>
        private void CheckQueue(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!IsExecuting)
            {
                CurrentThread = new Task(CanConsume);
                CurrentThread.Start();
            }


        }
        #endregion

        #region NormalState
        /// <summary>
        /// Verifica se há elementos na fila, busca o próximo cliente e executa
        /// </summary>
        public void CanConsume()
        {
            StartQueueMonitor();
            if (_execution.ExecutionQueue.Count > 0)
            {
                _checkQueue.Stop();
                _currentCustomer = GetNextCustomer();
                IsExecuting = true;
                Consume();
                IsExecuting = false;

                _checkQueue.Start();
            }


            EndQueueMonitor();



        }

        /// <summary>
        /// Adiciona +1 no tempo de execução do posto, cria as informações do cliente atual e executa
        /// </summary>
        public void Consume()
        {
            BuildCustomerInfo();
            IsExecuting = true;
            ExecuteCustomer();
            IsExecuting = false;

        }

        /// <summary>
        /// Verifica o estado da cadeira, caso esteja pendente entra em estado de pendente, caso não executa
        /// </summary>
        private void ExecuteCustomer()
        {
            StartSeatMonitor();

            if (_selectedSeat.Status == Status.Pending)
            {
                EnterPending();
                return;
            }

            CheckSteps();

        }
        #endregion

        #region PendingState
        /// <summary>
        /// Entra em estado para verificar quando o assento sairá do estado de pendente e para o timer de checar a fila
        /// </summary>
        private void EnterPending()
        {
            _checkQueue.Stop();

            _waitForPending.Start();

            EndSeatMonitor();
        }

        /// <summary>
        /// Verifica se a cadeira está em estado de pendente e caso não esteja mais executa
        /// </summary>
        public void CanConsumeSeat()
        {
            StartSeatMonitor();
            _selectedSeat = _execution.Sessions[_customerSessionIndex].Seats[_customerSeatIndex];
            var status = _selectedSeat.Status;

            if (status == Status.Pending)
            {
                EndSeatMonitor();
                return;
            }
            //Continua com o monitor fechado para não mudar o estado do assento
            Consume();
        }
        #endregion

        #region Execution
        /// <summary>
        /// Executa os passos do cliente
        /// </summary>
        private void CheckSteps()
        {

            char[] steps;
            bool seatIsUnavailableAndCustomerGaveUp = false;
            bool customerPaid = false;

            steps = _currentCustomer.Sequence.ToUpper().ToCharArray();

            
            foreach (var step in steps)
            {
                switch (step)
                {
                    //Na consulta verifica se o assento está disponível
                    case 'C':
                        //Caso o assento esteja indisponível e o cliente desistir para a execução dos passos
                        seatIsUnavailableAndCustomerGaveUp = !CheckIfSeatIsAvailable();
                        break;
                    //Seleciona o assento
                    case 'S':
                        ConfirmSeat();
                        break;
                    //Paga o assento
                    case 'P':
                        customerPaid =  Pay();
                        break;
                    //Cancela a execução do cliente
                    case 'X':
                        CancelCustomer();
                        break;

                }

                if (step == 'X' || seatIsUnavailableAndCustomerGaveUp)
                {
                    SetSeatStatus(Status.Available);
                    break;
                }
                    
            }


            //Adiciona o log do cliente e calcula se ele está novamente na fila
            AddStepLog?.Invoke(this, new StepLog
            {
                Customer = _currentCustomer,
                Session = _customerSession,
                Start = _execution.CurrentGlobalTime,
                Finish = _execution.CurrentGlobalTime + _currentCustomer.EstimatedTime,
                TryCounter = GetTryCounterFromCustomer(_currentCustomer),
                ThreadId = Id,
                Result = (customerPaid) ? CustomerResult.Confirm : CustomerResult.GaveUp

            });

            //caso a configuração de esperar o tempo do cliente esteja marcada esperado esse tempo (interface)
            if (_shouldWaitCustomerTime)
            {
                Thread.Sleep(_currentCustomer.EstimatedTime*1000);
            }
            //Adiciona no tempo do posto o tempo do cliente

            _execution.CurrentConsumersTime[Id - 1]+=_currentCustomer.EstimatedTime;

            //Adiciona no tempo de execução o tempo do cliente
            _execution.CurrentGlobalTime += _currentCustomer.EstimatedTime;

            //Chama a thread principal pra atualizar a interface (necessidade WPF)
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnCurrentGlobalTimeChanged?.Invoke(this, EventArgs.Empty);
            });
            
            //Adiciona o cliente na lista clientes finalizados
            _execution.ConsumersFinished.Add(_currentCustomer);



        }

        /// <summary>
        /// Pagamento do cliente, deixa a cadeira indisponível
        /// </summary>
        private bool Pay()
        {
            //Muda o status na sessão e adiciona no relatório a informação na thread principal (necessidade WPF)
            Application.Current.Dispatcher.Invoke(() =>
            {
                _selectedSeat.Status = Status.Unavailable;
                _selectedSeat.Customer = _currentCustomer;
                _selectedSeat.Color = Util.Red;

                _execution.Sessions[_customerSessionIndex].Seats[_customerSeatIndex] = _selectedSeat;
                AddReportLog?.Invoke(this,
                    $"Cliente {_currentCustomer.ArrivalTime} Posto {Id} {_currentCustomer.SelectedSeat} {_customerSession.StartTime:HH:mm} confirmou.");
            });

            return true;

        }
        /// <summary>
        /// Cancela a execução do cliente
        /// </summary>
        private void CancelCustomer()
        {
            //Adiciona a linha no relatório
            AddReportLog?.Invoke(this, $"Cliente {_currentCustomer.ArrivalTime} Posto {Id} {_currentCustomer.SelectedSeat} {_customerSession.StartTime:HH:mm} não confirmou.");

        }
        /// <summary>
        /// Confirma o assento
        /// </summary>
        private void ConfirmSeat()
        {


        }
        /// <summary>
        /// Verifica se o assento está disponível
        /// </summary>
        private bool CheckIfSeatIsAvailable()
        {
            //Verifica se o assento está disponível, caso esteja transforma o estado dele em pendente

            if (_selectedSeat.Status == Status.Available)
            {
                SetSeatStatus(Status.Pending);
                EndSeatMonitor();
                return true;
            }
            

            //Se não tiver disponível e o cliente quiser tentar outro
            if (_currentCustomer.OnUnavailableSeat == OnUnavailableSeatBehavior.TryAnother)
            {
                return TryAnotherSeat();

            }
            //Adiciona no relatório que o cliente desistiu
            AddReportLog?.Invoke(this, $"Cliente {_currentCustomer.ArrivalTime} Posto {Id} {_currentCustomer.SelectedSeat} {_customerSession.StartTime:HH:mm} desistiu.");

            return false;
        }

        /// <summary>
        /// Tenta outro assento
        /// </summary>
        private bool TryAnotherSeat()
        {
            StartSessionMonitor();
            //Verifica a próxima cadeira disponível
            var availableSeat = GetNextAvailableSeat();

            //Se tiver cadeiras disponíveis ele seleciona essa cadeira para o cliente e insere o cliente novamente na fila pra executar
            if (availableSeat != null)
            {
                _currentCustomer.SelectedSeat = availableSeat.Identifier;
                _execution.ExecutionQueue.Insert(0, _currentCustomer);
            }

            EndSessionMonitor();
            return false;

        }
       
        /// <summary>
        /// Retorna a próxima cadeira disponível
        /// </summary>
        /// <returns></returns>
        private Seat GetNextAvailableSeat() => _customerSession.Seats.FirstOrDefault(s => s.Status == Status.Available);

        /// <summary>
        /// Retorna qual é o número da tentiva do cliente
        /// </summary>
        /// <param name="customer">cliente</param>
        
        private int GetTryCounterFromCustomer(Customer customer)
        {
            var tryCounter = 0;

            //Verifica as tentativas desse cliente 
            if (_execution.Logs.Exists(l => l.Customer.ArrivalTime == customer.ArrivalTime))
                tryCounter = _execution.Logs.Where(l => l.Customer == customer).Max(c => c.TryCounter) + 1;

            return tryCounter;

        }


        #endregion

        #region Util


        private void SetSeatStatus(Status newStatus)
        {
            var newSeat = _execution.Sessions[_customerSessionIndex].Seats[_customerSeatIndex];
            newSeat.Status = newStatus;
            _selectedSeat = newSeat;
        }
        private void BuildCustomerInfo()
        {
            _customerSessionIndex = GetCustomerSessionIndex();
            _customerSeatIndex = GetCustomerSeatIndex();
            _customerSession = _execution.Sessions[_customerSessionIndex];
            _selectedSeat = _execution.Sessions[_customerSessionIndex].Seats[_customerSeatIndex];
         
        }
        private Customer GetNextCustomer()
        {
            StartQueueMonitor();
            var nextCustomer = _execution.ExecutionQueue.First();
            _execution.ExecutionQueue.RemoveAt(0);

            EndQueueMonitor();

            return nextCustomer;
        }
        private int GetCustomerSessionIndex()
        {
            var customerSessionIndex =
                _execution.Sessions.FindIndex(s => s.StartTime == _currentCustomer.SelectedSession);
            return customerSessionIndex;
        }
        private int GetCustomerSeatIndex()
        {
            var customerSessionIndex = GetCustomerSessionIndex();

            var customerSeatIndex = _execution.Sessions[customerSessionIndex].Seats
                .ToList().FindIndex(s => s.Identifier == _currentCustomer.SelectedSeat);

            return customerSeatIndex;
        }

        public void Finish()
        {
            _checkQueue.Stop();

        }

        private void StartSeatMonitor() => Monitor.Enter(_execution.Sessions[_customerSessionIndex].Seats[_customerSeatIndex]);
        private void EndSeatMonitor() => Monitor.Exit(_execution.Sessions[_customerSessionIndex].Seats[_customerSeatIndex]);

        private void StartQueueMonitor() => Monitor.Enter(_execution.ExecutionQueue);
        private void EndQueueMonitor() => Monitor.Exit(_execution.ExecutionQueue);

        private void StartSessionMonitor() => Monitor.Enter(_execution.Sessions[_customerSessionIndex]);
        private void EndSessionMonitor() => Monitor.Exit(_execution.Sessions[_customerSessionIndex]);
        #endregion



    }
}
