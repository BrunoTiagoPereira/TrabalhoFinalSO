using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WPF.App.Interfaces;

namespace WPF.App.Services
{
    //Serviço de navegação
    public class NavigationService : INavigationService<IBaseView>
    {
        //Provedor
        private readonly IServiceProvider _provider;

        //Construtor do serviço
        public NavigationService(IServiceProvider provider)
        {
            _provider = provider;
           
        }

        //Navegar para outra view sem parâmetros
        public void NavigateTo<T>()
        {
            //Pega a view de destino
            var source = (IBaseView)_provider.GetService<T>();

            //Recupera a janela principal
            var mainWindow = _provider.GetService<MainWindow>();

            //Define a view de destino ativa na janela principal
            mainWindow.ActiveView = source;
        }

        //Bavegar para outra view com parâmetros
        public void NavigateToWithParameter<T>(object parameter)
        {
            //Pega a view de destino
            var source = (IBaseView)_provider.GetService<T>();

            //Define os parâmetros da view de destino
            source.Parameter = parameter;

            //Recupera a janela principal
            var mainWindow = _provider.GetService<MainWindow>();

            //Define a view de destino ativa na janela principal
            mainWindow.ActiveView = source;
        }
    }
}
