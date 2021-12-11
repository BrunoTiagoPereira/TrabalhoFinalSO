using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF.App.Helpers;
using WPF.App.Public;

namespace WPF.App.Entities
{
    public class Command
    {

        public string Text { get; set; }
        public CommandType Type { get; set; }
        public CommandPriority CommandPriority => Util.GetCommandPriorityByType(Type);

    }
}
