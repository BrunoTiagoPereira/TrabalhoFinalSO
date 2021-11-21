using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPF.App.Entities;
using WPF.App.Interfaces;
using WPF.App.Public;

namespace WPF.App.Views
{
    /// <summary>
    /// Interaction logic for MovieTemplate.xaml
    /// </summary>
    public partial class MovieTemplate : Window
    {

        //Propriedades de binding 
        public ObservableCollection<Seat> Seats { get; set; }

        public MovieTemplate(IEnumerable<Seat> seats)
        {
        
            InitializeComponent();

            DataContext = this;

            Seats = new ObservableCollection<Seat>(seats);



        }

    }

   
}
