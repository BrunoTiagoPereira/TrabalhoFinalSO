using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF.App.Entities;

namespace WPF.App.Interfaces
{
    //Interface para o serviço de notificação
    public interface INotifyService
    {
        //Método de Alerta
        void Alert(Notification notification);

    }
}
