using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPF.App.Entities;
using WPF.App.Interfaces;
using WPF.App.Public;

namespace WPF.App.Views
{
    /// <summary>
    /// Interaction logic for MovieTemplate.xaml
    /// </summary>
    public partial class MovieTemplate : Window
    {
        public event EventHandler<string> _onCompiledCustomers; 
        private readonly Session _session;
        private readonly INotifyService _notifyService;


        private List<Customer> _executionQueue;

        private List<Customer> _customers;

        


        public ObservableCollection<Chair> Chairs { get; set; }

        public MovieTemplate(int rows, int columns, Session session, INotifyService _notifyService)
        {
            _session = session;
            this._notifyService = _notifyService;

            InitializeComponent();

            DataContext = this;

            _customers = session.Customers;
            _executionQueue = new List<Customer>();

            Chairs = new ObservableCollection<Chair>();

            //CreateRoom(columns, rows);

            _onCompiledCustomers += OnCompiledCustomers;



        }

        private void OnCompiledCustomers(object sender, string e)
        {
            _notifyService.Alert(new Notification(){Type = AlertType.Success, Text = e});
        }

        //private void CreateRoom(int columns, int rows)
        //{
        //    double width = 800;
        //    double height = 450;

        //    double ellipseWidth = width / columns;
        //    double ellipseHeight = height / rows;

        //    string prefix;

        //    for (int i = 0; i < rows; i++)
        //    {
        //        for (int j = 0; j < columns; j++)
        //        {
        //            prefix = ((j + 1) > 9) ? "" : "0";
        //            Chairs.Add(new Chair
        //            {
        //                Identifier = $"{NumberToString(i + 1)}{prefix}{j + 1}",
        //                Width = ellipseWidth,
        //                Height = ellipseHeight,
        //                Color = _blue,
        //                Top = i * (ellipseHeight + 10),
        //                Left = j * (ellipseWidth + 10),
        //                IsAvailable =  true,
        //            });

        //        }

        //    }


        //}


        public async Task Execute()
        {
            //foreach (var sessionCustomer in _session.Customers)
            //{
            //    var chairIndex = Chairs.IndexOf(Chairs.First(x => x.Identifier == sessionCustomer.SelectedSeat));

            //    Chairs[chairIndex].Color = _red;
            //}
            //return;
            await PremiumCustomers();
        }

        public async Task PremiumCustomers()
        {
            
            var premiumCustomers = _customers.Where(c => c.CustomerType == CustomerType.Premium).ToList();

            if (!premiumCustomers.Any()) return;

            FillExecutionQueue(c => c.CustomerType == CustomerType.Premium, premiumCustomers);

            CheckIfSeatsExist();

            ExecuteCustomersSteps();






          

            //Coloca os clientes que são premium na fila de execução e tira da fila total de clientes;
            //_executionQueue = new List<Customer>()
            _onCompiledCustomers?.Invoke(this, $"Foi executado(s) {premiumCustomers.Count()} cliente(s) premium");
        }

        //Terminar
        private void ExecuteCustomersSteps()
        {
            char[] steps;

            foreach (var customer in _executionQueue)
            {
                steps = customer.Sequence.ToUpper().ToCharArray();
                
                foreach (var step in steps)
                {
                    switch (step)
                    {
                        case 'C':
                            continue;
                            if (!IsSeatAvaiable(customer.SelectedSeat))
                            {

                            }
                                

                            break;
                        case 'S':
                            if (!IsSeatAvaiable(customer.SelectedSeat))
                            {

                            }
                            break;
                        case 'X':
                            break;

                    }
                }
            }
        }

        private bool IsSeatAvaiable(string identifier)
        {
            return Chairs.First(chair => chair.Identifier == identifier).IsAvailable;
        }

        private void CheckIfSeatsExist()
        {
            var customersToRemove = new List<Customer>();
            foreach (var customer in _executionQueue)
            {
                if(!Chairs.Any(chair=>chair.Identifier == customer.SelectedSeat))
                    customersToRemove.Add(customer);
            }

            _executionQueue.RemoveAll(c=>customersToRemove.Contains(c));
        }

        private void FillExecutionQueue(Predicate<Customer> func, IEnumerable<Customer> customersToAdd)
        {
            _customers.RemoveAll(func);
            _executionQueue.AddRange(customersToAdd);
            _executionQueue.OrderBy(c => c.ArrivalTime);
        }

    }

   
}
