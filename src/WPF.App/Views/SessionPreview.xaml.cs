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
using WPF.App.Entities;
using WPF.App.Interfaces;
using Timer = System.Timers.Timer;

namespace WPF.App.Views
{
    /// <summary>
    /// Interaction logic for SessionPreview.xaml
    /// </summary>
    public partial class SessionPreview : UserControl
    {
        //private readonly Session _session;
        //private SolidColorBrush _blue = new(Color.FromRgb(52, 140, 235));
        //private SolidColorBrush _red = new(Color.FromRgb(235, 52, 64));
        //private SolidColorBrush _green = new(Color.FromRgb(52, 235, 134));


        //public ObservableCollection<Chair> Chairs { get; set; }

        //public SessionPreview(int rows, int columns, Session session)
        //{
        //    _session = session;

        //    InitializeComponent();

        //    DataContext = this;

        //    Chairs = new ObservableCollection<Chair>();

        //    CreateRoom(columns, rows);



        //}


        //private void CreateRoom(int columns, int rows)
        //{
        //    double width = 800;
        //    double height = 450;

        //    double ellipseWidth = width / columns;
        //    double ellipseHeight = height / rows;

        //    string prefix;

        //    for (int i = 0; i < rows; i++)
        //    {
        //        for (int j = 0; j < columns; j++)
        //        {
        //            prefix = ((j + 1) > 9) ? "" : "0";
        //            Chairs.Add(new Chair
        //            {
        //                Identifier = $"{NumberToString(i + 1)}{prefix}{j + 1}",
        //                Width = ellipseWidth,
        //                Height = ellipseHeight,
        //                Color = _blue,
        //                Top = i * (ellipseHeight + 10),
        //                Left = j * (ellipseWidth + 10)
        //            });

        //        }

        //    }


        //}

        //private string NumberToString(int number)
        //{

        //    Char c = (Char)(97 + (number - 1));

        //    return c.ToString().ToUpper();

        //}



        //public async Task Execute()
        //{
        //    foreach (var sessionCustomer in _session.Customers)
        //    {
        //        var chairIndex = Chairs.IndexOf(Chairs.First(x => x.Identifier == sessionCustomer.SelectedSeat));

        //        Chairs[chairIndex].Color = _red;
        //    }
        //    return;
        //}
    }


    //public class RoomSize
    //{
    //    public int Rows { get; set; }
    //    public int Columns { get; set; }
    //}
    //public class Chair : INotifyPropertyChanged
    //{
    //    public SolidColorBrush Color
    //    {
    //        get => _color;
    //        set
    //        {
    //            _color = value;
    //            if (PropertyChanged != null)
    //                PropertyChanged(this, new PropertyChangedEventArgs("Color"));
    //        }
    //    }
    //    private SolidColorBrush _color;
    //    public string Identifier { get; set; }
    //    public double Width { get; set; }
    //    public double Height { get; set; }

    //    public double Top { get; set; }
    //    public double Left { get; set; }


    //    public event PropertyChangedEventHandler PropertyChanged;

    //}




}
