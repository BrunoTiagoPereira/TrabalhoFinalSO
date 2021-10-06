using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WPF.App.Helpers
{
    public static class Util
    {
        //Cor azul
        public static SolidColorBrush Blue => new(Color.FromRgb(52, 140, 235));

        //Cor vermelha
        public static SolidColorBrush Red => new(Color.FromRgb(235, 52, 64));

        //Cor Verde
        public static SolidColorBrush Green => new(Color.FromRgb(52, 235, 134));

        //Converter um número para o caractere correspondente ao número na tabela ASCII
        public static string NumberToString(int number)
        {
            Char c = (Char)(97 + (number - 1));

            return c.ToString().ToUpper();
        }
        //Método para pegar o caminho do arquivo para leitura
        public static string GetFileFromExplorer()
        {
            //Variável para receber o caminho do arquivo
            string filePath = null;

            //Abre a janela para informar o arquivo
            var openFileDialog = Factory.CreateOpenFileDialog();

            //Verifica se o arquivo foi informado 
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                //Se o arquivo for informado, define o valor da variável com o caminho do arquivo informado
                filePath = openFileDialog.FileName;

            //Retorna o caminho do arquivo
            return filePath;
        }

        //Método para pegar o caminho do arquivo para escrita
        public static string GetFileToCreateFromExplorer()
        {
            //Variável para receber o caminho do arquivo
            string filePath = null;

            //Abre a janela para informar o arquivo
            var saveFileDialog = Factory.CreateSaveFileDialog();

            //Verifica se o arquivo foi informado 
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                //Se o arquivo for informado, define o valor da variável com o caminho do arquivo informado
                filePath = saveFileDialog.FileName;

            //Retorna o caminho do arquivo
            return filePath;
        }

    }
}
