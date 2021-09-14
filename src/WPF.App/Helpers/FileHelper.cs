using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WPF.App.Helpers
{
    public static class FileHelper
    {

        //Método para ler o arquivo e retornar as linhas em um array de strings
        public static async Task<string[]> ReadFileAsync(string path)
        {
            var fileData = new List<string>();
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(fileStream))
                {
                   fileData.Add(await reader.ReadLineAsync());  
                }
            }

            return fileData.ToArray();
        }


    }
}
