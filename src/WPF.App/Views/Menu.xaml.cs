using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;
using WPF.App.Entities;
using WPF.App.Helpers;
using WPF.App.Interfaces;
using WPF.App.Public;

namespace WPF.App.Views
{
    /// <summary>
    /// Interaction logic for Menu.xaml
    /// </summary>
    public partial class Menu : IBaseView
    {
        private readonly INotifyService _notifiyService;
        private readonly INavigationService<IBaseView> _navigationService;
        private readonly IReport _report;
        private readonly IExecution _execution;
        public object Parameter { get; set; }
        public Type TypeScreen { get; set; }

        #region MainObjects

        public List<Customer> Customers { get; set; }
        public List<Session> Sessions { get; set; }
        public int RowDimension { get; set; }
        public int ColumnDimension { get; set; }


       
        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty CurrentTimeProperty = DependencyProperty.Register(
            "CurrentTime", typeof(int), typeof(Menu), new PropertyMetadata(0));

        public int CurrentTime
        {
            get { return (int)GetValue(CurrentTimeProperty); }
            set { SetValue(CurrentTimeProperty, value); }
        }
        public static readonly DependencyProperty ShouldWaitForNextCustomerProperty = DependencyProperty.Register(
            "ShouldWaitForNextCustomer", typeof(bool), typeof(Menu), new PropertyMetadata(false));

        public bool ShouldWaitForNextCustomer
        {
            get { return (bool)GetValue(ShouldWaitForNextCustomerProperty); }
            set { SetValue(ShouldWaitForNextCustomerProperty, value); }
        }

        public static readonly DependencyProperty ShouldWaitCustomerTimeProperty = DependencyProperty.Register(
            "ShouldWaitCustomerTime", typeof(bool), typeof(Menu), new PropertyMetadata(false));

        public bool ShouldWaitCustomerTime
        {
            get { return (bool)GetValue(ShouldWaitCustomerTimeProperty); }
            set { SetValue(ShouldWaitCustomerTimeProperty, value); }
        }

        public int RoomConfigLineCount
        {
            get { return (int)GetValue(RoomConfigLineCountProperty); }
            set { SetValue(RoomConfigLineCountProperty, value); }
        }

        //Contador de linhas lidas do arquivo de Clientes
        public static readonly DependencyProperty CustomersLineCountProperty = DependencyProperty.Register(
            "CustomersLineCount", typeof(int), typeof(Menu), new PropertyMetadata(0));

        public int CustomersLineCount
        {
            get { return (int)GetValue(CustomersLineCountProperty); }
            set { SetValue(CustomersLineCountProperty, value); }
        }

        //Contador de linhas lidas do arquivo de configurações das Salas/Sessões
        public static readonly DependencyProperty RoomConfigLineCountProperty = DependencyProperty.Register(
            "RoomConfigLineCount", typeof(int), typeof(Menu), new PropertyMetadata(0));


        public static readonly DependencyProperty IsContentLoadedProperty = DependencyProperty.Register(
            "IsContentLoaded", typeof(bool), typeof(Menu), new PropertyMetadata(false));

        public bool IsContentLoaded
        {
            get { return (bool) GetValue(IsContentLoadedProperty); }
            set { SetValue(IsContentLoadedProperty, value); }
        }

        public static readonly DependencyProperty EnableImportProperty = DependencyProperty.Register(
            "EnableImport", typeof(bool), typeof(Menu), new PropertyMetadata(true));


        public bool EnableImport
        {
            get { return (bool) GetValue(EnableImportProperty); }
            set { SetValue(EnableImportProperty, value); }
        }


        public static readonly DependencyProperty SelectThreadsCounterProperty = DependencyProperty.Register(
            "SelectThreadsCounter", typeof(ThreadListItem), typeof(Menu), new PropertyMetadata(null));

        public ThreadListItem SelectThreadsCounter
        {
            get { return (ThreadListItem)GetValue(SelectThreadsCounterProperty); }
            set { SetValue(SelectThreadsCounterProperty, value); }
        }

        public static readonly DependencyProperty ThreadsListItemsProperty = DependencyProperty.Register(
            "ThreadsListItems", typeof(ObservableCollection<ThreadListItem>), typeof(Menu), new PropertyMetadata(default(ObservableCollection<ThreadListItem>)));

        public ObservableCollection<ThreadListItem> ThreadsListItems
        {
            get { return (ObservableCollection<ThreadListItem>)GetValue(ThreadsListItemsProperty); }
            set { SetValue(ThreadsListItemsProperty, value); }
        }
        
        #endregion

        public Menu(INotifyService notifyService, IReport report, IExecution execution, INavigationService<IBaseView> navigationService)
        {
            InitializeComponent();

   
            Sessions = new List<Session>();
            Customers = new List<Customer>();
       

            _notifiyService = notifyService;
            _report = report;
            _execution = execution;
            _navigationService = navigationService;

            DataContext = this;
            _report.OnReportFinished += OnReportFinished;
            _report.OnCurrentGlobalTimeChange += OnCurrentGlobalTimeChanged;
            InstanceThreadsList();
            

        }

        private void OnCurrentGlobalTimeChanged(object sender, EventArgs e)
        {
            CurrentTime = _execution.CurrentGlobalTime;
        }
        #region Instance
        /// <summary>
        /// Cria a lista de threads disponíveis para a escolha
        /// </summary>
        public void InstanceThreadsList()
        {
            ThreadsListItems = new ObservableCollection<ThreadListItem>();
            //Verifica a quantiadade de Threads disponíveis pra execução e busca 0.001% delas
            var maxAvaiableThreads = Util.GetMaxThreadsAvailable();

            for (int i = 1; i <= maxAvaiableThreads; i++)
            {
                ThreadsListItems.Add(new ThreadListItem { Text= (i != 1) ? $"{i} cabines" : $"{i} cabine", Value=i});
            }
        }
        #endregion
        #region Events

        private void OnReportFinished(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsContentLoaded = true;
                _notifiyService.Alert(new Notification { Type = AlertType.Success, Text = "Algoritmo executado com sucesso!" });
            });

        }
        #endregion


        #region FileHandle


        /// <summary>
        /// Importa o arquivo de configuração
        /// </summary>
        private async void OpenRoomConfigFile(object sender, RoutedEventArgs e)
        {
            //Pegando o caminho do arquivo
            string filePath = Util.GetFileFromExplorer();
            string line;
            List<string> roomConfiguration = new List<string>();

            //Se o caminho do arquivo for nulo , sair do método
            if (filePath == null) return;

            //Leitor de arquivo
            using (var reader = Factory.CreateStreamReaderFromFile(filePath))
            {
                //Enquanto houver linha
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    //Armazena a linha na lista 
                    roomConfiguration.Add(line);
                    //Adiciona um no contador de linhas de do arquivo de Salas/Sessões
                    RoomConfigLineCount++;
                }
            }

            //Chamar método de importação as configurações de Sala/Sessões
            await ImportRoomConfig(roomConfiguration);

        }

        /// <summary>
        /// Importa o arquivo de clientes
        /// </summary>
        private async void OpenCustomersFile(object sender, RoutedEventArgs e)
        {
            //Pegando o caminho do arquivo
            string filePath = Util.GetFileFromExplorer();
            string line;
            List<string> customers = new List<string>();

            //Se o caminho do arquivo for nulo , sair do método
            if (filePath == null) return;

            //Leitor de arquivo
            using (var reader = Factory.CreateStreamReaderFromFile(filePath))
            {
                //Enquanto houver linha
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    //Armazena a linha na lista 
                    customers.Add(line);
                    //Adiciona um no contador de linhas de do arquivo de Clientes
                    CustomersLineCount++;
                }
            }

            //Chamar método de importação de Clientes
            await ImportCustomers(customers);

        }

        #endregion

        #region Helpers
        /// <summary>
        /// Método para importar as configurações de Sala/Sessões
        /// </summary>
        /// <param name="roomConfiguration">Dados da configuração das salas</param>
        private async Task ImportRoomConfig(List<string> roomConfiguration)
        {
            //Valida se as linhas estão no formato incorreto e , se sim, retorna mensagem para usuário
            if (!ValidationHelper.ValidateRoomConfig(roomConfiguration))
            {
                _notifiyService.Alert(new Notification { Type = AlertType.Error, Text = "Arquivo de configuração de salas em formato inválido" });
                RoomConfigLineCount = 0;
                return;
                
            }
               
            //Modelo: 10x20                     #filas x cadeiras
            //        14:30, 17:00, 20:30       #sessões
            Session session;

            //Variáveis armazenando as informações de dimensão e de sessões
            var dimensionConfig = roomConfiguration[0].Split("x");
            var sessionsConfigs = roomConfiguration[1].Split(",");

            int rows = int.Parse(dimensionConfig[0]);
            int columns = int.Parse(dimensionConfig[1]);


            RowDimension = rows;
            ColumnDimension = columns;

            //Passa em cada sessão informada
            foreach (var sessionStartTime in sessionsConfigs)
            {
                //Criando entidade de Sessão
                session = new Session
                {
                    Id = Guid.NewGuid(),
                    StartTime = DateTime.Parse(sessionStartTime)
                };

                //Adicionando nova sessão a lista de Sessões
                Sessions.Add(session);

            }

            //Alerta que a configuração foi importada
            _notifiyService.Alert(new Notification { Type = AlertType.Success, Text = $"Configuração importada!" });

            //Verificando a importação
            CheckImport();

        }

        /// <summary>
        /// Método para importar os Clientes
        /// </summary>
        /// <param name="customers">Dados dos clientes</param>
        private async Task ImportCustomers(List<string> customers)
        {
            //Variável para armazenar os erros 
            var erro = new StringBuilder();
            string[] customerInfo;

            //Variável auxiliar de Cliente
            Customer customer;

            //Passa pelas linhas de clientes
            for (int i = 0; i < customers.Count; i++)
            {
                //Valida se a linha está no formato incorreto e ,se sim, adiciona um erro na variável de armazenamento de erros
                if (!ValidationHelper.ValidateCustomer(customers[i]))
                {
                    erro.Append(i + ((i == customers.Count - 1) ? "" : ","));
                    continue;
                }

                //Modelo: J07;17:00;CSP;T;R;7

                //Variáveis armazenando as informações do cliente
                customerInfo = customers[i].Split(";");

                //Criando entidade de Cliente
                customer = new Customer
                {
                    SelectedSeat = customerInfo[0],
                    SelectedSession = DateTime.Parse(customerInfo[1]),
                    Sequence = customerInfo[2],
                    OnUnavailableSeat = Customer.GetOnUnavailableSeatBehaviorFromIdentifier(customerInfo[3]),
                    ArrivalTime = i + 1,
                    CustomerType = Customer.GetCustomerTypeFromIdentifier(customerInfo[4]),
                    EstimatedTime = int.Parse(customerInfo[5]),
                    TimeWaitForNext = int.Parse(customerInfo[6])

                };

                //Adicionando novo cliente a lista de Clientes
                Customers.Add(customer);
            }

            //Verifica se existe algum erro e ,se sim, retorna as mensagens de erro
            if (!string.IsNullOrWhiteSpace(erro.ToString()))
            {
                _notifiyService.Alert(new Notification { Type = AlertType.Error, Text = $"Erros nos clientes: {erro.ToString()}" });
                CustomersLineCount = 0;
                
                return;
            }

            //Alerta que os clientes foram importados
            _notifiyService.Alert(new Notification { Type = AlertType.Success, Text = $"Clientes importados!" });
            
            //Atualiza o contador de linhas de Clientes, importando apenas os validados corretamente
            CustomersLineCount = Customers.Count;

            //Verificando a importação 
            CheckImport();
        }

        /// <summary>
        /// Verificar a importação e se estiver tudo certo, executa o algoritmo do relatório
        /// </summary>
        private void CheckImport()
        {
            //Se ainda possuir sessões ou clientes, definir arquivo como não carregado ainda
            if (Customers.Count == 0 || Sessions.Count == 0 || SelectThreadsCounter == null)
                return;

            EnableImport = false;

            ////Inclui os clientes na sessão
            Util.CreateSessionsSeats(_execution,Sessions,ColumnDimension,RowDimension);

            _notifiyService.Alert(new Notification{Type = AlertType.Warning, Text = "Executando o algortimo..."});
            //Executa o algoritmo do relatório
            _report.Build(Customers,SelectThreadsCounter.Value, ShouldWaitForNextCustomer,ShouldWaitCustomerTime);

        }



        /// <summary>
        /// Mostra o resultado do algoritmo na sessão
        /// </summary>
        private void NavigateToSession(object sender, RoutedEventArgs e)
        {
            var sessionId = (Guid)((Button)sender).Tag;
            var session = Sessions.First(s => s.Id == sessionId);
            var preview = new MovieTemplate(session.Seats);
            preview.Show();


        }
        #endregion

        /// <summary>
        /// Gera o relatório
        /// </summary>
        private async void GenerateReport(object sender, RoutedEventArgs e)
        {
            await _report.Generate();
        }

        private void OnThreadsCounterChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckImport();
        }

        private void GoToCommandLine(object sender, RoutedEventArgs e)
        {
            _navigationService.NavigateTo<CommandLine>();
        }
    }

    public class ThreadListItem
    {
        public string Text { get; set; }
        public int Value { get; set; }
    }
}
