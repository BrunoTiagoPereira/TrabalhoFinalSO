using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.App.Public
{
    //Tipos de alerta
    public enum  AlertType
    {
        Success = 1,
        Warning = 2,
        Error = 3
    }
    //Tipos de comando
    public enum CommandType
    {
        Log,
        ConsumersCount,
        ChangeFileOutputName,
        Update,
        FileInputPath,
        Simulate,
        Totalize,
        Finish
    }

    //Prioridade do comando
    public enum CommandPriority
    {
        Critical,
        Standard,
        NoNeed

    }

    //Resultado do cliente
    public enum CustomerResult
    {
        Confirm,
        GaveUp
    }
}
