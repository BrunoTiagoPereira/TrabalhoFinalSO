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


        //Horário atual global de todos os postos
        public int CurrentGlobalTime { get; set; }
    }
}