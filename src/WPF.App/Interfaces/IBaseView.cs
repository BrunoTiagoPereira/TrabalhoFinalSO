using System;
using System.Windows.Controls;

namespace WPF.App.Interfaces
{
    //Interface para a base das views
    public interface IBaseView
    {
        //Parametro da view
        public object Parameter { get; set; }

        //Tipo de tela da view
        public Type TypeScreen{ get; set; }
    }
}
