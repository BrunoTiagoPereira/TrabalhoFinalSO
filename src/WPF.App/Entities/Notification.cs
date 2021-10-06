using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF.App.Public;

namespace WPF.App.Entities
{
    //Notificação: Classe auxiliar para notificação do usuário
    public class Notification
    {
        //Texto
        public string Text { get; set; }

        //Tipo de Alerta
        public AlertType Type { get; set; }
    }



}
