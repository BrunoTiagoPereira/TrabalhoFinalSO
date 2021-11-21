﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPF.App.Helpers;
using WPF.App.Interfaces;
using Timer = System.Timers.Timer;

namespace WPF.App.Entities
{
    public class Consumer
    {
        private readonly IExecution _execution;

        private const int TimerInterval = 1000;
        public Task CurrentThread { get; set; }
        public bool IsExecuting { get; private set; }

        public int Id { get; set; }

        private Timer _checkQueue;

        private Timer _waitForPending;
    
       
        private Customer _currentCustomer;

        private int _consumerTime;

        
        private int _customerSessionIndex;
        private int _customerSeatIndex;
        private Session _customerSession;
        private Seat _selectedSeat;

        #region Events

        public static event EventHandler<StepLog> AddStepLog; 
        public static event EventHandler<string> AddReportLog; 
        #endregion
        public Consumer(IExecution execution, int id)
        {
            Id = id;
            this._execution = execution;
            _consumerTime = 0;
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


        private void WaitForPending(object sender, System.Timers.ElapsedEventArgs e)
        {
            CanConsumeSeat();
        }

        private void CheckQueue(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!IsExecuting)
            {
                CurrentThread = new Task(CanConsume);
                CurrentThread.Start();
            }
           
        }

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
        public void Consume()
        {
            BuildCustomerInfo();
            IsExecuting = true;
            ExecuteCustomer();
            IsExecuting = false;
        }

        private void ExecuteCustomer()
        {
            StartSeatMonitor();

            if (_selectedSeat.Status == Status.Pending)
            {
                EnterPending();
                return;
            }

            //if(!_checkQueue.Enabled)
            //    _checkQueue.Start();


            CheckSteps();

        }

        private void EnterPending()
        {
            _checkQueue.Stop();
            
            _waitForPending.Start();
            
            EndSeatMonitor();
        }

        

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


        #region Execution
        /// <summary>
        /// Executa os passos do cliente
        /// </summary>
        private void CheckSteps()
        {

            char[] steps;
            bool seatIsUnavailableAndCustomerGaveUp = false;

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
                        Pay();
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
                ThreadId = Id

            });

            //Adiciona no tempo de execução o tempo do cliente
            _execution.CurrentGlobalTime += _currentCustomer.EstimatedTime;
            _consumerTime ++;

            _execution.ConsumersFinished.Add(_currentCustomer);



        }

        /// <summary>
        /// Pagamento do cliente, deixa a cadeira indisponível
        /// </summary>
        private void Pay()
        {
            //Muda o status na sessão e adiciona no relatório a informação
            _selectedSeat.Status = Status.Unavailable;
            _selectedSeat.Customer = _currentCustomer;
            _selectedSeat.Color = Util.Red;

            _execution.Sessions[_customerSessionIndex].Seats[_customerSeatIndex] = _selectedSeat;
            AddReportLog?.Invoke(this, $"Cliente {_currentCustomer.ArrivalTime} Posto {Id} {_currentCustomer.SelectedSeat} {_customerSession.StartTime:HH:mm} confirmou.");
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
            //Verifica se o assento está disponível

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
