using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPF.App.Entities;
using WPF.App.Interfaces;
using WPF.App.Services;
using WPF.App.Views;
using NavigationService = WPF.App.Services.NavigationService;

namespace WPF.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //Configuração de injeção de dependência
        private ServiceProvider _serviceProvider;
        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
          
            services.AddTransient<Menu>();
            services.AddTransient<INavigationService<IBaseView>,NavigationService>();
            services.AddTransient<INotifyService,NotifyService>();
            services.AddSingleton<IReport, Report>();
            services.AddSingleton<IExecution, Execution>();

        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow.ActiveView = _serviceProvider.GetService<Menu>();
            mainWindow.Show();
        }
    }
}
