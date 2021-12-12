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


        //Tarefa para gerar o arquivo do relatório
        public Task Generate();
        //Gera o relatório para o caminho especificado e retorna se foi válido a mensagem
        public Task<(bool valid, string message)> Generate(string filePath);
        public List<string> ReportLines { get; set; }

        //Tarefa para construir o relatório
        public void Build(List<Customer> customers, int threads, bool shouldWaitForNextCustomer, bool shouldWaitCustomerTime);

        public event EventHandler OnReportFinished;
        //Evento quando o tempo da aplicação for alterado
        public event EventHandler OnCurrentGlobalTimeChange;
    }
}
