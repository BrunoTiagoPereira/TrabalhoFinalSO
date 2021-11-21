using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WPF.App.Helpers;
using WPF.App.Interfaces;
using WPF.App.Public;
using Timer = System.Timers.Timer;

namespace WPF.App.Entities
{
    //Relatório
    public class Report : IReport
    {
        #region Props
        //Serviço de notificação
        private readonly INotifyService _notifyService;
        //Singleton de execução
        private readonly IExecution _execution;

        //Tempo que começou a executar
        private DateTime _start;


        //Lista de registros
        public List<StepLog> Logs { get; set; }


        //Lista de produtores
        private Producer _producer;


        //Lista de consumidores
        private List<Consumer> _consumers;
        private Timer _checkFinished;


        //Lista de linhas de relatório
        public List<string> ReportLines { get; set; }
        #endregion

        #region Events
        //Evento acionado com o relatório terminar de executar
        public event EventHandler OnReportFinished;

        //Evento quando o tempo da aplicação for alterado
        public event EventHandler OnCurrentGlobalTimeChange;
        #endregion

        //Construtor
        public Report(INotifyService notifyService, IExecution execution)
        {
            //Definindo o serviço de notificação
            _notifyService = notifyService;
            _execution = execution;

            //Inicializando variáveis
            _start = DateTime.Now;

            _consumers = new List<Consumer>();

            Logs = new List<StepLog>();
            ReportLines = new List<string>();


            _execution.ProducerFinished = new List<Customer>();
            _execution.ExecutionQueue = new List<Customer>();
            _execution.ConsumersFinished = new List<Customer>();
          
            _execution.Logs = new List<StepLog>();

            Consumer.AddStepLog += ConsumerAddStepLog;
            Consumer.AddReportLog += ConsumerAddReportLog;
            Consumer.OnCurrentGlobalTimeChanged += OnCurrentGlobalTimeChanged;



        }
        #region EventHandlers
        private void OnCurrentGlobalTimeChanged(object sender, EventArgs e)
        {
            OnCurrentGlobalTimeChange?.Invoke(this, e);
        }

        private void ConsumerAddReportLog(object sender, string e)
        {
            ReportLines.Add(e);
        }

        private void ConsumerAddStepLog(object sender, StepLog e)
        {
            _execution.Logs.Add(e);
        }
        #endregion


        #region Instances

        private void InstanceCheckQueueFinished()
        {
            _checkFinished = new Timer();
            _checkFinished.Enabled = true;
            _checkFinished.Interval = 1000;
            _checkFinished.Elapsed += CheckFinished;
            _checkFinished.Start();


        }

        private void CheckFinished(object sender, ElapsedEventArgs e)
        {
            Monitor.Enter(_execution.Logs);
            Monitor.Enter(_execution.ProducerFinished);

            var executionLog = _execution.Logs;
            var executionProducerFinished = _execution.ProducerFinished;
            if (executionProducerFinished.Count==executionLog.Count(l => l.TryCounter==0)&&
                !_producer.IsExecuting)
            {
                _consumers.ForEach(c=>c.Finish());
                _checkFinished.Stop();
                WriteThreadsResult();
                Monitor.Exit(_execution.Logs);
                OnReportFinished?.Invoke(null,null);
                return;
            }

            Monitor.Exit(_execution.Logs);
            Monitor.Exit(_execution.ProducerFinished);
        }

        #endregion

        /// <summary>
        /// Executa o algoritmo do relatório
        /// </summary>
        /// <param name="customers">Lista de clientes</param>
        /// <param name="threads">quantidade de threads para a execução</param>
        /// <param name="shouldWaitForNextCustomer">esperar o tempo até o próximo cliente chegar</param>
        /// <param name="shouldWaitCustomerTime">esperar o tempo do cliente executando</param>
        public void Build(List<Customer> customers, int threads, bool shouldWaitForNextCustomer, bool shouldWaitCustomerTime)
        {
            //Cria o produtor
            CreateProducer(customers, shouldWaitForNextCustomer);

            //Cria os consumidores
            CreateConsumers(threads, shouldWaitCustomerTime);

            //Timer pra verificar quando terminou o processo
            InstanceCheckQueueFinished();

        }

        /// <summary>
        /// Para cada thread passada como parâmetro cria um consumidor
        /// </summary>
        /// <param name="threads">número de threads escolhido</param>
        /// <param name="shouldWaitCustomerTime">esperar o tempo do cliente</param>
        private void CreateConsumers(int threads, bool shouldWaitCustomerTime)
        {
            _execution.CurrentConsumersTime = new List<int>();
            for (int i = 0; i < threads; i++)
            {
                this._consumers.Add(new Consumer(_execution,i+1, shouldWaitCustomerTime));
                _execution.CurrentConsumersTime.Add(0);
            }
        }

        /// <summary>
        /// Cria o produtor
        /// </summary>
        /// <param name="customers">Clientes</param>
        /// <param name="shouldWaitForNextCustomer">esperar o tempo até o próximo cliente</param>
        private void CreateProducer(List<Customer> customers, bool shouldWaitForNextCustomer)
        {
            _producer = new Producer(customers, _execution, shouldWaitForNextCustomer);
        }


        /// <summary>
        /// Gera o relatório
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

        /// <summary>
        /// Escreve o resultado final do relatório
        /// </summary>
        private void WriteThreadsResult()
        {
            int total = _execution.CurrentConsumersTime.Sum();
            int lastThreadEnd = _execution.CurrentConsumersTime.Max();
            ReportLines.Add($"Horário de início: {_start:HH:mm:ss tt}");
            ReportLines.Add($"Horário de finalização: {_start.AddMinutes(lastThreadEnd):HH:mm:ss tt}");
            ReportLines.Add("");
            ReportLines.Add($"Horário de finalização por posto:");

            for (int i = 0; i < _consumers.Count; i++)
            {
                ReportLines.Add($"Posto {_consumers[i].Id}: {_start.AddMinutes(_execution.CurrentConsumersTime[i]):HH:mm:ss tt} - Minutos gastos: {_execution.CurrentConsumersTime[i]}");
            }
            ReportLines.Add("");
            ReportLines.Add($"Tempo gasto total: {total}");


        }

    }
}
