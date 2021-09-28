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
    public class NotifyService : INotifiyService
    {

        private readonly IServiceProvider _provider;


        public NotifyService(IServiceProvider provider)
        {
            _provider = provider;
           
        }

        public void Alert(Notification notification)
        {
            var mainWindow = _provider.GetService<MainWindow>();

            mainWindow.NotificationColor = GetColorFromAlert(notification.Type);

            mainWindow.Snackbar.MessageQueue.Enqueue(notification.Text);
        }

        private SolidColorBrush GetColorFromAlert(AlertType alertType)
        {
            SolidColorBrush color = null;
            switch (alertType)
            {
                case AlertType.Error:
                    color = new SolidColorBrush(Color.FromRgb(255, 77, 64));
                    break;
                case AlertType.Success:
                    color = new SolidColorBrush(Color.FromRgb(0, 163, 33));
                    break;
            }

            return color;
        }




    }
}
