using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WPF.App.Interfaces;

namespace WPF.App.Services
{
    public class NavigationService : INavigationService<IBaseView>
    {
        private readonly IServiceProvider _provider;

        public NavigationService(IServiceProvider provider)
        {
            _provider = provider;
           
        }
        public void NavigateTo<T>()
        {
            var source = (IBaseView)_provider.GetService<T>();
            var mainWindow = _provider.GetService<MainWindow>();
            mainWindow.ActiveView = source;
        }

        public void NavigateToWithParameter<T>(object parameter)
        {
            var source = (IBaseView)_provider.GetService<T>();
            source.Parameter = parameter;
            var mainWindow = _provider.GetService<MainWindow>();
            mainWindow.ActiveView = source;
        }
    }
}
