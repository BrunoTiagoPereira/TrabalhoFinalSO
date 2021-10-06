using System;

namespace WPF.App.Interfaces
{
    //Interface para o serviço de navegação
    public interface INavigationService<T> where T:IBaseView
    {
        //Método de navegação para outra view sem parâmetros
        public void NavigateTo<T>();

        //Método de navegação para outra view com parâmetros
        public void NavigateToWithParameter<T>(object parameter);
    }
}
