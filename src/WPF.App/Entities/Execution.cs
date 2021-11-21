using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF.App.Interfaces;

namespace WPF.App.Entities
{
    public class Execution : IExecution
    {
        //Fila de execução
        public List<Customer> ExecutionQueue { get; set; }
        //Sessões criadas pra execução
        public List<Session> Sessions { get; set; }

        //Logs de execução
        public List<StepLog> Logs { get; set; }

        //Lista de finalizados pelos consumidores
        public List<Customer> ConsumersFinished { get; set; }

        //Lista de finalizados pelos produtores
        public List<Customer> ProducerFinished { get; set; }

        //Horário atual global de todos os postos
        public int CurrentGlobalTime { get; set; }

        //Evento quando o cliente precisa adicionar um cliente em uma nova cadeira
        public event EventHandler<Customer> TryAnotherSeat;

        //Tempo de cada thread
        public List<int> CurrentConsumersTime { get; set; }
    }
}
