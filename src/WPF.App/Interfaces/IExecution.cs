using System;
using System.Collections.Generic;
using WPF.App.Entities;

namespace WPF.App.Interfaces
{
    public interface IExecution
    {
        public List<Customer> ExecutionQueue { get; set; }
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
    }
}