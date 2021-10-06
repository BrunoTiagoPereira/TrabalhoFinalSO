using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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

        //Horário atual
        private int _currentTime;

        //Lista de registros
        public List<StepLog> Logs { get; set; }

        //Lista de sessões
        public List<Session> Sessions { get; set; }

        //Lista de linhas de relatório
        public List<string> ReportLines { get; set; }

        //
        private const decimal HalfPricePercentage = 0.4M;

        //Construtor
        public Report(INotifyService notifyService)
        {
            //Definindo o serviço de notificação
            _notifyService = notifyService;

            //Inicializando variáveis
            _executionQueue = new List<Customer>();
            _currentTime = 1;
            Logs = new List<StepLog>();
            ReportLines = new List<string>();

        }

        //Gerar arquivo de relatório
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

        public async Task Build()
        {
            var sessions = Sessions.OrderBy(s => s.StartTime);
            foreach (var session in sessions)
            {
                PremiumCustomers(session);
                HalfPriceCustomers(session);
                RegularCustomers(session);

            }
        }

        private void RegularCustomers(Session session)
        {
            var regularCustomers = session.Customers.Where(c => c.CustomerType == CustomerType.Regular).ToList();

            if (!regularCustomers.Any()) return;

            FillExecutionQueue(regularCustomers);

            CheckIfSeatsExist(session);

            ExecuteCustomersSteps(session, false);
        }

        private void HalfPriceCustomers(Session session)
        {
            var halfPriceCustomers = session.Customers.Where(c => c.CustomerType == CustomerType.HalfPrice).ToList();

            if (!halfPriceCustomers.Any()) return;

            FillExecutionQueue(halfPriceCustomers);

            CheckIfSeatsExist(session);

            ExecuteCustomersSteps(session, true);
        }

        public void PremiumCustomers(Session session)
        {

            var premiumCustomers = session.Customers.Where(c => c.CustomerType == CustomerType.Premium).ToList();

            if (!premiumCustomers.Any()) return;

            FillExecutionQueue(premiumCustomers);

            CheckIfSeatsExist(session);

            ExecuteCustomersSteps(session,false);


        }

        private void ExecuteCustomersSteps(Session session, bool validateHalfPricePercentage)
        {
            var chairsCount = session.Chairs.Count;
            var sessionIndex = Sessions.IndexOf(session);
            bool moreThanMaximumHalfPrice;
            while (_executionQueue.Count > 0)
            {
                moreThanMaximumHalfPrice = (decimal)Sessions[sessionIndex].Chairs.Count(c => !c.IsAvailable) / chairsCount > HalfPricePercentage;

                if (validateHalfPricePercentage && moreThanMaximumHalfPrice)
                    return;

                CheckSteps(_executionQueue[0], session);
            }



        }

        private void CheckSteps(Customer customer, Session session)
        {

            _executionQueue.Remove(customer);

            char[] steps;

            steps = customer.Sequence.ToUpper().ToCharArray();

            foreach (var step in steps)
            {
                switch (step)
                {
                    case 'C':
                        CheckIfSeatIsAvailable(customer, session);
                        break;
                    case 'S':
                        ConfirmSeat(customer, session);
                        break;
                    case 'P':
                        Pay(customer,session);
                        break;
                    case 'X':
                        CancelCustomer(customer, session);
                        break;
                    
                }

                if (step == 'X')
                    break;
            }

            Logs.Add(new StepLog
            {
                Customer = customer,
                Session =  session,
                Start = _currentTime,
                Finish = _currentTime + customer.EstimatedTime,
                TryCounter = GetTryCounterFromCustomer(customer)

            });

            _currentTime += customer.EstimatedTime;

            

        }

        private void Pay(Customer customer, Session session)
        {
            var chair = session.Chairs.First(x => x.Identifier == customer.SelectedSeat);
            var chairIndex = session.Chairs.IndexOf(chair);
            var sessionIndex = Sessions.IndexOf(session);

            chair.IsAvailable = false;
            chair.Customer = customer;

            Sessions[sessionIndex].Chairs[chairIndex] = chair;
            ReportLines.Add($"Cliente {customer.ArrivalTime} {customer.SelectedSeat} {session.StartTime:HH:mm} confirmou.");
        }

        private void CancelCustomer(Customer customer, Session session)
        {
            ReportLines.Add($"Cliente {customer.ArrivalTime} {customer.SelectedSeat} {session.StartTime:HH:mm} não confirmou.");

        }


        private void ConfirmSeat(Customer customer, Session session)
        {
           

        }

        private void CheckIfSeatIsAvailable(Customer customer, Session session)
        {
            var isAvailable = session.Chairs.First(chair => chair.Identifier == customer.SelectedSeat).IsAvailable;


            if (isAvailable)
                return;


            if (customer.OnUnavailableSeat == OnUnavailableSeatBehavior.TryAnother)
            {
                TryAnotherChair(customer,session);
                return;
            }

            ReportLines.Add($"Cliente {customer.ArrivalTime} {customer.SelectedSeat} {session.StartTime:HH:mm} desistiu.");
        }

        private void TryAnotherChair(Customer customer, Session session)
        {
            var availableChair = GetNextAvailableChair(session);

            //Não há mais cadeiras disponíveis
            if (availableChair == null)
                return;

            customer.SelectedSeat = availableChair.Identifier;
            _executionQueue.Insert(0, customer);
        }

        private int GetTryCounterFromCustomer(Customer customer)
        {
            var tryCounter = 0; 

            //Verifica as tentativas desse cliente 
            if (Logs.Exists(l => l.Customer.ArrivalTime == customer.ArrivalTime))
                tryCounter = Logs.Where(l => l.Customer == customer).Max(c => c.TryCounter) + 1;

            return tryCounter;

        }
        private Chair GetNextAvailableChair(Session session)
        {
            return session.Chairs.FirstOrDefault(c => c.IsAvailable);
        }


        private void CheckIfSeatsExist(Session session)
        {
            var customersToRemove = new List<Customer>();
            foreach (var customer in _executionQueue)
            {
                if (!session.Chairs.Any(chair => chair.Identifier == customer.SelectedSeat))
                    customersToRemove.Add(customer);
            }

            _executionQueue.RemoveAll(c => customersToRemove.Contains(c));
        }

        private void FillExecutionQueue(IEnumerable<Customer> customersToAdd)
        {
            _executionQueue.AddRange(customersToAdd);
            _executionQueue.OrderBy(c => c.ArrivalTime);
        }
    }
}
