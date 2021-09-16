using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.App.Entities
{
    public class Session
    {

        public Session()
        {
            MovieRoom = new MovieRoom();
        }
        public MovieRoom MovieRoom { get; set; }

        public DateTime StartTime { get; set; }     
    }
}
