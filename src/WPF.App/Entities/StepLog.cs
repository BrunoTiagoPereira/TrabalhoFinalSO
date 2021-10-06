using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.App.Entities
{
    //Registro de passo
    public class StepLog
    {
        //Cliente autor 
        public Customer Customer { get; set; }

        //Sessão envolvida
        public Session Session { get; set; }

        //Inicio
        public int Start { get; set; }
        //Fim
        public int Finish { get; set; }

        //Tentativas
        public int TryCounter { get; set; }
    }
}
