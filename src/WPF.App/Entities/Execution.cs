using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF.App.Interfaces;

namespace WPF.App.Entities
{
    public class Execution : IExecution
    {
        public List<Customer> ExecutionQueue { get; set; }
        public List<Session> Sessions { get; set; }
        public List<StepLog> Logs { get; set; }
        public List<Customer> ConsumersFinished { get; set; }
        public List<Customer> ProducerFinished { get; set; }
        public int CurrentGlobalTime { get; set; }
    }
}
