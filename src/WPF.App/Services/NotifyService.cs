using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using WPF.App.Entities;
using WPF.App.Interfaces;
using WPF.App.Public;

namespace WPF.App.Services
{
    //Serviço de notificação
    public class NotifyService : INotifyService
    {
        //Provedor
        private readonly IServiceProvider _provider;

        //Construtor do serviço
        public NotifyService(IServiceProvider provider)
        {
            _provider = provider;
           
        }

        //Alerta
        public void Alert(Notification notification)
        {
            //Recupera a janela principal
            var mainWindow = _provider.GetService<MainWindow>();

            //Define coloração da notificação a partir do tipo da mesma
            mainWindow.NotificationColor = GetColorFromAlert(notification.Type);

            //Exibe o a notificação(Alerta)
            mainWindow.Snackbar.MessageQueue.Enqueue(notification.Text);
        }

        //Pegar cor do alerta
        private SolidColorBrush GetColorFromAlert(AlertType alertType)
        {
            //Variavel auxiliar
            SolidColorBrush color = null;

            //Switch para identificar qual a cor do alerta a partir do tipo do alerta
            switch (alertType)
            {
                case AlertType.Error:
                    color = new SolidColorBrush(Color.FromRgb(255, 77, 64));
                    break;
                case AlertType.Success:
                    color = new SolidColorBrush(Color.FromRgb(0, 163, 33));
                    break;
            }

            //Retorna variavel auxiliar com a cor
            return color;
        }


    }
}
