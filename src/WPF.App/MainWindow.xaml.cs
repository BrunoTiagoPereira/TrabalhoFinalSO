using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using WPF.App.Interfaces;

namespace WPF.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //Propriedades de binding 
        #region DependencyProperties

        public static readonly DependencyProperty ActiveViewProperty = DependencyProperty.Register(
            "ActiveView", typeof(IBaseView), typeof(MainWindow), new PropertyMetadata(default(IBaseView)));

        public IBaseView ActiveView
        {
            get { return (IBaseView) GetValue(ActiveViewProperty); }
            set { SetValue(ActiveViewProperty, value); }
        }

        public static readonly DependencyProperty NotificationColorProperty = DependencyProperty.Register(
            "NotificationColor", typeof(SolidColorBrush), typeof(MainWindow), new PropertyMetadata(default(SolidColorBrush)));

        public SolidColorBrush NotificationColor
        {
            get { return (SolidColorBrush) GetValue(NotificationColorProperty); }
            set { SetValue(NotificationColorProperty, value); }
        }
        #endregion



        public MainWindow()
        {

            InitializeComponent();
            DataContext = this;
            Snackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

        }

    }
}
