using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
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
        private ServiceProvider serviceProvider;
        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            services.AddTransient<Menu>();
            services.AddTransient<SessionPreview>();
            services.AddTransient<INavigationService<IBaseView>,NavigationService>();
            services.AddTransient<INotifiyService,NotifyService>();
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = serviceProvider.GetService<MainWindow>();
            mainWindow.ActiveView = serviceProvider.GetService<Menu>();
            mainWindow.Show();
        }
    }
}
