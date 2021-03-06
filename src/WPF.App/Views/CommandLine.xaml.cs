using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
using WPF.App.Entities;
using WPF.App.Helpers;
using WPF.App.Interfaces;
using WPF.App.Public;
using Console = System.Console;

namespace WPF.App.Views
{
    
    /// <summary>
    /// Interaction logic for CommandLine.xaml
    /// </summary>
    public partial class CommandLine : IBaseView
    {

        #region Props
        private readonly IExecution _execution;
        private readonly IReport _report;
        public object Parameter { get; set; }
        public Type TypeScreen { get; set; }


        //Caminho do arquivo de output da simulação
        public string OutputFileName { get; set; } = "ResultadoVendaClientes.txt";
        //Quantidade de postos
        public int ConsumersCount { get; set; } = 1;
        //Deve logar o resultado no console
        public bool ShouldLogInConsole { get; set; } = true;
        //Dimensão das linhas das sessões
        public int RowDimension { get; set; }
        //Dimensão das colunas das sessões
        public int ColumnDimension { get; set; }
        //Quantidade de linhas importadas
        public int ImportLineCount { get; set; }
        //Clientes
        public List<Customer> Customers { get; set; }
        //Sessões
        public List<Session> Sessions { get; set; }
        //Últimos comandos usados
        public Stack<string> UsedCommands { get; set; }


        #endregion

        #region DependencyProperties
        public static readonly DependencyProperty EnableImportProperty = DependencyProperty.Register(
            "EnableImport", typeof(bool), typeof(CommandLine), new PropertyMetadata(true));


        public bool EnableImport
        {
            get { return (bool)GetValue(EnableImportProperty); }
            set { SetValue(EnableImportProperty, value); }
        }

        public ObservableCollection<CommandLineText> Commands { get; set; }


        #endregion

        #region Ctor
        public CommandLine(IExecution execution, IReport report)
        {
            _execution = execution;
            _report = report;
            InitializeComponent();

            Sessions = new List<Session>();
            Customers = new List<Customer>();
            DataContext = this;
            Commands = new ObservableCollection<CommandLineText>();
            UsedCommands = new Stack<string>();
            _report.OnReportFinished += OnReportFinished;
            CommandInput.Focus();
            Keyboard.Focus(CommandInput);
        }
        #endregion


        #region Execution
        /// <summary>
        /// Executa o comando
        /// </summary>
        /// <param name="text">texto do comando</param>
        private void ExecuteCommand(string text)
        {
            var commands = new List<Command>();
            //Valida o comando
            var validation = ValidationHelper.ValidateDevToolsCommand(text, out commands);

            //Se tiver erros retorna os erros
            if (validation.HasErrors)
            {
                Commands.Add(new CommandLineText
                {
                    Text = validation.Errors.Select(e => e.Error).Aggregate((a, b) => $"{a}{Environment.NewLine}{b}"),
                    Color = Util.Red
                });
                return;
            }

            //Executa os comandos por prioridade
            ExecuteCommandsByPriority(commands);


        }

        /// <summary>
        /// Executa os comandos por prioridade
        /// </summary>
        /// <param name="commands">comandos</param>
        private void ExecuteCommandsByPriority(List<Command> commands)
        {
            var noNeed = commands.Where(c => c.CommandPriority == CommandPriority.NoNeed);
            var standard = commands.Where(c => c.CommandPriority == CommandPriority.Standard);
            var critical = commands.Where(c => c.CommandPriority == CommandPriority.Critical);

            ExecuteNoNeed(noNeed);
            ExecuteStandard(standard);
            ExecuteCritical(critical);
        }

        /// <summary>
        /// Executa os comandos sem prioridade
        /// </summary>
        /// <param name="noNeed">comandos</param>
        private void ExecuteNoNeed(IEnumerable<Command> noNeed)
        {
            //Verifica o tipo, busca o valor no comando e define as variáveis
            foreach (var cmd in noNeed)
            {
                switch (cmd.Type)
                {
                    case CommandType.Log:
                        ShouldLogInConsole = cmd.Text.ToLower().Contains("tela");
                        break;
                    case CommandType.ConsumersCount:
                        ConsumersCount = int.Parse(Regex.Match(cmd.Text, @"\d{1,}").Captures[0].Value);
                        break;
                    case CommandType.ChangeFileOutputName:
                        OutputFileName = Regex.Match(cmd.Text, @""".{1,}\.txt""").Captures[0].Value.Replace("\"", "");
                        break;
                }

                Commands.Add(new CommandLineText
                {
                    Color = Util.Green,
                    Text = $"Comando '{cmd.Text}' executado com sucesso!"
                });
            }
        }

        /// <summary>
        /// Executa os comandos padrões
        /// </summary>
        /// <param name="standard">comandos padrões</param>
        private void ExecuteStandard(IEnumerable<Command> standard)
        {
            //Busca os comandos de inserir arquivos
            var insertFileCommands = standard.Where(c => c.Type == CommandType.FileInputPath);

            foreach (var cmd in insertFileCommands)
            {
                //Busca o caminho do arquivo
                var fileName = Regex.Match(cmd.Text, @""".{1,}\.txt""").Captures[0].Value.Replace("\"", "");
                try
                {
                    //Abre o arquivo e importa as informações
                    if (OpenFile(fileName))
                    {
                        Commands.Add(new CommandLineText
                        {
                            Color = Util.Green,
                            Text = $"Comando '{cmd.Text}' executado com sucesso!"
                        });
                    }
                    else
                    {
                        Commands.Add(new CommandLineText
                        {
                            Color = Util.Red,
                            Text = $"Não foi possível executar o comando '{cmd.Text}'."
                        });
                    }

                }
                catch (Exception)
                {
                    Commands.Add(new CommandLineText
                    {
                        Color = Util.Red,
                        Text = $"não foi possível ler o arquivo do comando '{cmd.Text}', verifique se o arquivo é valido e se o caminho está corretamente preenchido."
                    });
                }

            }

        }

        /// <summary>
        /// Executa os comandos críticos
        /// </summary>
        /// <param name="critical">comandos críticos</param>
        private void ExecuteCritical(IEnumerable<Command> critical)
        {
            var hasFinishCommand = critical.ToList().Exists(c => c.Type == CommandType.Finish);
            var hasSimulateCommand = critical.ToList().Exists(c => c.Type == CommandType.Simulate);
            var hasTotalizeCommand = critical.ToList().Exists(c => c.Type == CommandType.Totalize);

            //Tem comandos para finalizar
            if (hasFinishCommand)
                Application.Current.Shutdown();

            //Tem comandos para simular
            if (hasSimulateCommand)
                Simulate();

            //Verifica se ao totalizar já foi executado o algortimo
            if (hasTotalizeCommand)
            {
                if (_execution.Logs.Count == 0)
                {
                    Commands.Add(new CommandLineText
                    {
                        Color = Util.Red,
                        Text = $"O algortimo não foi executado"
                    });

                }
                else
                {
                    Totalize();
                }
            }

        }


        #endregion






        #region FileHandle


        /// <summary>
        /// Importa o arquivo
        /// </summary>
        private bool OpenFile(string filePath)
        {
            
            if (!File.Exists(filePath))
            {
                Commands.Add(new CommandLineText
                {
                    Color = Util.Red,
                    Text = $"O arquivo '{filePath}' não existe no diretório do app"
                });
                return false;
            }

            string[] configuration = File.ReadAllLines(filePath);
            ImportLineCount = configuration.Length;

            if (configuration.Length > 0)
            {
                var roomConfigValidation = configuration.Length == 2 &&
                                                ValidationHelper.ValidateRoomConfig(new List<string> { configuration[0], configuration[1]});
                var customersValidations = ValidationHelper.ValidateCustomer(configuration[0]);

                if (customersValidations)
                {
                    //Chamar método de importação as dos clientes
                    ImportCustomers(configuration);
                }
                else if(roomConfigValidation)
                {
                    //Chamar método de importação as configurações de Sala/Sessões
                    ImportRoomConfig(configuration);
                }
                else
                {
                    Commands.Add(new CommandLineText
                    {
                        Color = Util.Red,
                        Text = "O arquivo informado não é valido para configuração das salas e clientes!"
                    });
                }

                return true;

            }

            Commands.Add(new CommandLineText
            {
                Color = Util.Red,
                Text = "O arquivo informado está vazio"
            });

            return false;



        }

        #endregion

        #region Helpers
        /// <summary>
        /// Método para importar as configurações de Sala/Sessões
        /// </summary>
        /// <param name="roomConfiguration">Dados da configuração das salas</param>
        private void ImportRoomConfig(string[] roomConfiguration)
        {
            Sessions.Clear();
            _execution.Sessions?.Clear();
            //Valida se as linhas estão no formato incorreto e , se sim, retorna mensagem para usuário
            if (!ValidationHelper.ValidateRoomConfig(roomConfiguration.ToList()))
            {
                Commands.Add(new CommandLineText
                {
                    Color = Util.Red,
                    Text = "Arquivo de configuração de salas em formato inválido"
                });
                ImportLineCount = 0;
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
            Commands.Add(new CommandLineText
            {
                Color = Util.Green,
                Text = "Configuração importada!"
            });

        }

        /// <summary>
        /// Método para importar os Clientes
        /// </summary>
        /// <param name="customers">Dados dos clientes</param>
        private void ImportCustomers(string[] customers)
        {
            Customers.Clear();
            //Variável para armazenar os erros 
            var erro = new StringBuilder();
            string[] customerInfo;

            //Variável auxiliar de Cliente
            Customer customer;

            //Passa pelas linhas de clientes
            for (int i = 0; i < customers.Length; i++)
            {
                //Valida se a linha está no formato incorreto e ,se sim, adiciona um erro na variável de armazenamento de erros
                if (!ValidationHelper.ValidateCustomer(customers[i]))
                {
                    erro.Append(i + ((i == customers.Length - 1) ? "" : ","));
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
                Commands.Add(new CommandLineText
                {
                    Color = Util.Red,
                    Text = $"Erros nos clientes: {erro.ToString()}"
                });
                ImportLineCount = 0;

                return;
            }

            //Alerta que os clientes foram importados
            Commands.Add(new CommandLineText
            {
                Color = Util.Green,
                Text = $"Clientes importados!"
            });

            //Atualiza o contador de linhas de Clientes, importando apenas os validados corretamente
            ImportLineCount = Customers.Count;

        }

        /// <summary>
        /// Verificar a importação e se estiver tudo certo, executa o algoritmo do relatório
        /// </summary>
        private bool Simulate()
        {
            //Se ainda possuir sessões ou clientes, definir arquivo como não carregado ainda
            if (Customers.Count == 0 || Sessions.Count == 0)
            {
                Commands.Add(new CommandLineText
                {
                    Color = Util.Red,
                    Text = "Clientes ou sessões não importados!"
                });
                return false;
            }

            //Limpa as últimas execuções
            ClearPreviousExecution();

            //Inclui os clientes na sessão
            Util.CreateSessionsSeats(_execution, Sessions, ColumnDimension, RowDimension);

            Commands.Add(new CommandLineText
            {
                Text = "Executando algoritmo"
            });
            //Executa o algoritmo do relatório
            _report.Build(Customers, ConsumersCount, false, false);

          

            return true;

        }

    
        /// <summary>
        /// Gera o relatório beseado no tipo de output
        /// </summary>
        private async void GenerateReport()
        {

            if (ShouldLogInConsole)
            {
                foreach (var line in _report.ReportLines)
                {
                    Commands.Add(new CommandLineText
                    {
                        Color = Util.Green,
                        Text = line,
                        ShowDevText = false
                    });
                }
                
                
                return;
            }
            var result =  await _report.Generate(OutputFileName);

            Commands.Add(new CommandLineText
            {
                Color = (result.valid)?Util.Green:Util.Red,
                Text = result.message,
                ShowDevText = false
            });

            ScrollToEnd();

        }

        /// <summary>
        /// Totaliza os clientes que desistiram e confirmaram e mostra no console
        /// </summary>
        private void Totalize()
        {
            string confirmed = $"Clientes confirmados: {_execution.Logs.Count(l => l.Result == CustomerResult.Confirm)}";
            string gaveUps = $"Clientes que desistiram: {_execution.Logs.Count(l => l.Result == CustomerResult.GaveUp)}";
            Commands.Add(new CommandLineText
            {
                Color = Util.Green,
                Text = confirmed,
                ShowDevText = false
            });
            Commands.Add(new CommandLineText
            {
                Color = Util.Green,
                Text = gaveUps,
                ShowDevText = false
            });
        }


        #endregion


        #region Util
        /// <summary>
        /// Escreve o texto de ajuda
        /// </summary>
        private void WriteHelper()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("Comandos disponíveis:");
            sb.AppendLine("clear -> limpa o console");
            sb.AppendLine("help -> mostra os comandos disponíveis");
            sb.AppendLine("f1 -> preenche a linha de com o último comando utilizado");
            sb.AppendLine("-log 'arquivo|tela' -> muda o tipo de log");
            sb.AppendLine("-pontos 'quantidade' -> define a quantidade de pontos/threads de consumidores");
            sb.AppendLine("alterar -in \"nome_arquivo\" -> insere um arquivo de configuração");
            sb.AppendLine("-out \"nome_arquivo\" -> define o nome do arquivo de saída, caso o log seja em arquivo");
            sb.AppendLine("simular -> executa o processo");
            sb.AppendLine("totalizar -> totaliza a quantidade de confirmações e desistências");
            sb.AppendLine("finalizar -> finaliza a aplicação");
            sb.AppendLine("-----------------------------------------------------------");
            Commands.Add(new CommandLineText
            {
                Text = sb.ToString(),
                Color = Util.Green
            });
        }
        /// <summary>
        /// Leva a tela para o final
        /// </summary>
        private void ScrollToEnd()
        {
            CommandListBox.SelectedIndex = Commands.Count - 1;
            CommandListBox.ScrollIntoView(CommandListBox.SelectedItem);
        }
        private void ClearPreviousExecution()
        {
            _execution.ProducerFinished = new List<Customer>();
            _execution.ExecutionQueue = new List<Customer>();
            _execution.ConsumersFinished = new List<Customer>();
            _execution.CurrentConsumersTime = _execution?.CurrentConsumersTime?.Select(c => 0).ToList();
            _execution.CurrentGlobalTime = 0;
            _execution.Logs = new List<StepLog>();
        }


        #endregion

        #region Events
        //Quando é finalizado a execução do relatório
        private void OnReportFinished(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(GenerateReport);
        }

        /// <summary>
        /// Quando é pressionado uma tecla
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPressKey(object sender, KeyEventArgs e)
        {
            //Se for enter executa o comando
            if (e.Key == Key.Enter)
            {
                //Adiciona o comando como executado
                string text = CommandInput.Text;
                CommandInput.Text = "";
                Commands.Add(new CommandLineText() { Text = text });


                //Limpa o console
                if (text.ToLower() == "clear")
                {
                    Commands.Clear();
                    return;
                }

                //Escreve o testo de ajuda
                if (text.ToLower() == "help")
                {
                    WriteHelper();
                    return;
                }

                //Se o comando não for nulo adiciona nos últimos comandos utilizados
                if (!string.IsNullOrWhiteSpace(text))
                    UsedCommands.Push(text);

                //Executa o comando
                ExecuteCommand(text);

                //leva a tela para o final
                ScrollToEnd();

            }
            //Se for F1 preenche o texto com o útlimo comando utilizado
            if (e.Key == Key.F1 && UsedCommands.Count > 0)
                CommandInput.Text = UsedCommands.Pop();


        }

        #endregion
    }

    public class CommandLineText
    {
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public SolidColorBrush Color { get; set; } = Util.ConsoleDefaultColor;

        public bool ShowDevText { get; set; } = true;
    }
}
