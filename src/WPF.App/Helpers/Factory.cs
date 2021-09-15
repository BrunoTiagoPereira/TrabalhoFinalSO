using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WPF.App.Helpers
{
    public static class Factory
    {
        //Cria a janela para o usuário enviar o arquivo
        public static OpenFileDialog CreateOpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                InitialDirectory = "c:\\",
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true

            };

            return openFileDialog;
        }

        //Cria a stream do arquivo e retorna o leitor da stream do arquivo
        public static StreamReader CreateFileStream(string path)
        {
            //Verifica se arquivo existe no caminho informado e , se não, retorna exceção
            if (!File.Exists(path))
                throw new FileNotFoundException($"Arquivo '{path}' não encontrado.");

            //Cria uma stream do arquivo informado
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            //Leitor da stream do arquivo
            var streamReader = new StreamReader(fileStream);

            return streamReader;
        }


    }
}
