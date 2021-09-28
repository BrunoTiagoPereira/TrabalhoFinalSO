using System;

namespace WPF.App.Interfaces
{
    public interface INavigationService<T> where T:IBaseView
    {
        public void NavigateTo<T>();
        public void NavigateToWithParameter<T>(object parameter);
    }
}
