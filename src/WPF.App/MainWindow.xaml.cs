using System;
using System.Data;
using WPF.App.Entities;
using WPF.App.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        #region DependencyProperty
        public static readonly DependencyProperty ConfigFileLineCountProperty = DependencyProperty.Register(
            "ConfigFileLineCount", typeof(int), typeof(MainWindow), new PropertyMetadata(default(int)));

        public int ConfigFileLineCount
        {
            get { return (int)GetValue(ConfigFileLineCountProperty); }
            set { SetValue(ConfigFileLineCountProperty, value); }
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            
        }


        private async void OpenRoomConfigFile(object sender, RoutedEventArgs e)
        {
            var filePath = string.Empty;

            var openFileDialog = Factory.CreateOpenFileDialog();

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
            }
            var retorno = await FileHelper.ReadFileAsync(filePath);

             



        }




        

    }
}
