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
        //Lista de sessões
        public List<Session> Sessions { get; set; }

        //Tarefa para gerar o arquivo do relatório
        public Task Generate();

        //Tarefa para construir o relatório
        public Task Build();
    }
}
