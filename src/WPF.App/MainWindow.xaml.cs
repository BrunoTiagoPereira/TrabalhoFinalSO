using System;
using System.Data;
using WPF.App.Entities;
using WPF.App.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;

namespace WPF.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region MainObjects

        public List<Customer> Customers { get; set; }
        public List<Session> Sessions { get; set; }

        #endregion



        #region DependencyProperties
        //Contador de linhas lidas do arquivo de configurações das Salas/Sessões
        public static readonly DependencyProperty RoomConfigLineCountProperty = DependencyProperty.Register(
                "RoomConfigLineCount", typeof(int), typeof(MainWindow), new PropertyMetadata(0));

        public int RoomConfigLineCount
        {
            get { return (int)GetValue(RoomConfigLineCountProperty); }
            set { SetValue(RoomConfigLineCountProperty, value); }
        }

        //Contador de linhas lidas do arquivo de Clientes
        public static readonly DependencyProperty CustomersLineCountProperty = DependencyProperty.Register(
            "CustomersLineCount", typeof(int), typeof(MainWindow), new PropertyMetadata(0));

        public int CustomersLineCount
        {
            get { return (int)GetValue(CustomersLineCountProperty); }
            set { SetValue(CustomersLineCountProperty, value); }
        }

        #endregion



        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            Sessions = new List<Session>();
            Customers = new List<Customer>();

            Snackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));
        }



        //Função do botão de Salas/Sessões para importar arquivo de dados
        private async void OpenRoomConfigFile(object sender, RoutedEventArgs e)
        {

            //Variável contadora de linhas do arquivo de Salas/Sessões
            RoomConfigLineCount = 0;

            //Pegando o caminho do arquivo
            string filePath = GetFileFromExplorer();
            string line;
            List<string> roomConfiguration = new List<string>();

            //Se o caminho do arquivo for nulo , sair do método
            if (filePath == null) return;

            //Leitor de arquivo
            using (var reader = Factory.CreateFileStream(filePath))
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

        //Função do botão de Clientes para importar arquivo de dados
        private async void OpenCustomersFile(object sender, RoutedEventArgs e)
        {
            //Variável contadora de linhas do arquivo de Clientes
            CustomersLineCount = 0;

            //Pegando o caminho do arquivo
            string filePath = GetFileFromExplorer();
            string line;
            List<string> customers = new List<string>();

            //Se o caminho do arquivo for nulo , sair do método
            if (filePath == null) return;

            //Leitor de arquivo
            using (var reader = Factory.CreateFileStream(filePath))
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



        #region Execution

        

        #endregion



        #region Helpers

        //Método para pegar o caminho do arquivo
        private string GetFileFromExplorer()
        {
            //Variável para receber o caminho do arquivo
            string filePath = null;

            //Abre a janela para informar o arquivo
            var openFileDialog = Factory.CreateOpenFileDialog();

            //Verifica se o arquivo foi informado 
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                //Se o arquivo for informado, define o valor da variável com o caminho do arquivo informado
                filePath = openFileDialog.FileName;

            //Retorna o caminho do arquivo
            return filePath;
        }



        //Método para importar as configurações de Sala/Sessões
        private async Task ImportRoomConfig(List<string> roomConfiguration)
        {
            //Valida se as linhas estão no formato incorreto e , se sim, retorna mensagem para usuário
            if (!ValidationHelper.ValidateRoomConfig(roomConfiguration))
                Snackbar.MessageQueue.Enqueue("Arquivo de configuração de salas em formato inválido");

            //Modelo: 10x20                     #filas x cadeiras
            //        14:30, 17:00, 20:30       #sessões


            Session session;

            //Variáveis armazenando as informações de dimensão e de sessões
            var dimensionConfig = roomConfiguration[0].Split("x");
            var sessionsConfigs = roomConfiguration[1].Split(",");

            int rows = int.Parse(dimensionConfig[0]);
            int columns = int.Parse(dimensionConfig[1]);

            //Criando entidade de Sala
            var movieRoom = new MovieRoom()
            {
                Room = new Seat[rows, columns],
            };

            //Passa em cada sessão informada
            foreach (var sessionStartTime in sessionsConfigs)
            {
                //Criando entidade de Sessão
                session = new Session
                {
                    MovieRoom = movieRoom,
                    StartTime = DateTime.Parse(sessionStartTime)
                };

                //Adicionando nova sessão a lista de Sessões
                Sessions.Add(session);

            }

        }

        //Método para importar os Clientes
        private async Task ImportCustomers(List<string> customers)
        {
            //Variável para armazenar os erros 
            var erro = new StringBuilder();
            string[] customerInfo;

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
                    OnUnavaibleSeat = Customer.GetOnUnavaibleSeatBehaviorFromIdentifer(customerInfo[3]),
                    ArrivalTime = i,
                    CustomerType =  Customer.GetCustomerTypeFromIdentifier(customerInfo[4]),
                    EstimatedTime = int.Parse(customerInfo[5])
                    
                };

                //Adicionando novo cliente a lista de Clientes
                Customers.Add(customer);
            }

            //Verifica se existe algum erro e ,se sim, retorna as mensagens de erro
            if (!string.IsNullOrWhiteSpace(erro.ToString()))
                Snackbar.MessageQueue.Enqueue($"Erros nos clientes: {erro.ToString()}");

        }

        #endregion


    }
}
