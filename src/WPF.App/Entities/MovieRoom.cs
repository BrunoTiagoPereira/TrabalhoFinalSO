using System;
using System.Collections.Generic;

namespace WPF.App.Entities
{
    public class MovieRoom
    {
        public MovieRoom()
        {
            Room = new Seat[10,10];
        }
        public Seat[,] Room { get; set; }//10,20 
    }


}
