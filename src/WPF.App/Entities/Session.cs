using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.App.Entities
{
    //Sessão
    public class Session
    {
        //Identificador
        public Guid Id { get; set; }

        //Coleção de objetos cadeira observaveis 
        public ObservableCollection<Chair> Chairs { get; set; }

        //Lista de clientes
        public List<Customer> Customers { get; set; }

        //Data de inicio
        public DateTime StartTime { get; set; }

        public bool HasCustomers
        {
            get => Customers?.Count > 0;
        }
    }
}
