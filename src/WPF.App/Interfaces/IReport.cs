using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF.App.Entities;

namespace WPF.App.Interfaces
{
    //Interface para o relatório
    public interface IReport
    {

        //Logs de execução
        public List<StepLog> Logs { get; set; }

        //Tarefa para gerar o arquivo do relatório
        public Task Generate();

        //Tarefa para construir o relatório
        public void Build(List<Session> sessions);
    }
}
