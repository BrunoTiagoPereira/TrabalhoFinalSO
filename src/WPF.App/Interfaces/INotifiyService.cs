using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF.App.Entities;

namespace WPF.App.Interfaces
{
    public interface INotifiyService
    {
        void Alert(Notification notification);
    }
}
