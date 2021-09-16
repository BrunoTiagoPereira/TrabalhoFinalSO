using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPF.App.Annotations;
using WPF.App.Entities;
using Timer = System.Timers.Timer;

namespace WPF.App.Views
{
    /// <summary>
    /// Interaction logic for SessionPreview.xaml
    /// </summary>
    public partial class SessionPreview : UserControl
    {

        private SolidColorBrush _blue = new (Color.FromRgb(52, 140, 235));
        private SolidColorBrush _red = new (Color.FromRgb(235, 52, 64));
        private SolidColorBrush _green = new (Color.FromRgb(52, 235, 134));


        public ObservableCollection<Chair> Chairs { get; set; }


        public int Counter { get; set; }
        public SessionPreview()
        {
            
            InitializeComponent();

            DataContext = this;

            Chairs = new ObservableCollection<Chair>();


            int columns = 10;
            int rows = 10;

            CreateRoom(columns, rows);




        }


        private void CreateRoom(int columns, int rows)
        {
            int width = 800;
            int height = 450;

            double ellipseWidth =  width / columns;
            double ellipseHeight = height / rows;   

            string prefix;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    prefix = ((j+1) > 9) ? "" : "0";
                    Chairs.Add(new Chair
                    {
                        Identifier = $"{NumberToString(i+1)}{prefix}{j+1}",
                        Width = ellipseWidth,
                        Height = ellipseHeight,
                        Color = _blue,
                        Top = i * (ellipseHeight + 10),
                        Left = j * (ellipseWidth + 10)
                    });

                }

            }
           
        }

        private int LetterToNumber(char letter)
        {
            int index = char.ToUpper(letter) - 64;
            return index;
        }


        private string NumberToString(int number)

        {

            Char c = (Char)(97 + (number - 1));

            return c.ToString().ToUpper();

        }

    }

    public class Chair : INotifyPropertyChanged
    {
        public SolidColorBrush Color
        {
            get => _color;
            set
            {
                _color = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Color"));
            }
        }
        private SolidColorBrush _color;
        public string Identifier { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double Top { get; set; }
        public double Left { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

    }

}
