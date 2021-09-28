using System;
using System.Windows.Controls;

namespace WPF.App.Interfaces
{
    public interface IBaseView
    {
        public object Parameter { get; set; }
        public Type TypeScreen{ get; set; }
    }
}
