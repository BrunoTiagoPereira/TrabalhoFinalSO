using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using WPF.App.Entities;
using WPF.App.Interfaces;
using WPF.App.Public;

namespace WPF.App.Helpers
{
    public static class Util
    {
        public const decimal MaxAvailableThreadsPercentage = 0.001m;

        //Cor azul
        public static SolidColorBrush Blue => new(Color.FromRgb(52, 140, 235));

        //Cor vermelha
        public static SolidColorBrush Red => new(Color.FromRgb(235, 52, 64));

        //Cor Azul console
        public static SolidColorBrush ConsoleDefaultColor = new(Color.FromRgb(42, 87, 156));

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
            if (openFileDialog.ShowDialog() == DialogResult.OK)
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
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                //Se o arquivo for informado, define o valor da variável com o caminho do arquivo informado
                filePath = saveFileDialog.FileName;

            //Retorna o caminho do arquivo
            return filePath;
        }

        //Método que busca as threads disponíveis no computador
        public static int GetMaxThreadsAvailable()
        {
            var workerThreads = 0;
            var portThreads = 0;
            ThreadPool.GetAvailableThreads(out workerThreads, out portThreads);

            var maxAvaiableThreads = (int)(workerThreads * MaxAvailableThreadsPercentage);

            return maxAvaiableThreads;
        }
        /// <summary>
        /// Cria as cadeiras para as salas
        /// </summary>
        /// <param name="columns">quantidade de colunas</param>
        /// <param name="rows">quantidade de linhas</param>
        /// <returns>Lista de cadeiras</returns>
        private static ObservableCollection<Seat> CreateSeats(int columns, int rows)
        {
            var seats = new ObservableCollection<Seat>();
            double width = 800;
            double height = 450;

            double ellipseWidth = width / columns;
            double ellipseHeight = height / rows;

            string prefix;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    prefix = ((j + 1) > 9) ? "" : "0";
                    seats.Add(new Seat
                    {
                        Identifier = $"{Util.NumberToString(i + 1)}{prefix}{j + 1}",
                        Width = ellipseWidth,
                        Height = ellipseHeight,
                        Color = Util.Blue,
                        Top = i * (ellipseHeight + 10),
                        Left = j * (ellipseWidth + 10),
                        Status = Status.Available,
                    });

                }

            }

            return seats;

        }

        /// <summary>
        /// Cria os assentos das sessões
        /// </summary>
        public static void CreateSessionsSeats(IExecution execution,List<Session> sessions, int columnDimension, int rowDimension)
        {
            foreach (var session in sessions)
            {
                session.Seats = CreateSeats(columnDimension, rowDimension);
            }

            execution.Sessions = sessions;
        }

        /// <summary>
        /// Padroniza e cria os comandos baseado em um texto de comandos
        /// </summary>
        /// <param name="commandText">texto do comando</param>
        /// <returns>Lista de comandos em texto</returns>
        /// <exception cref="InvalidOperationException">Caso alguma comando seja inválido lança uma execeção</exception>
        public static List<string> GetNormalizedCommands(string commandText)
        {
            var commands = new List<string>();
            //Separa os comandos por espaço e filtra os se naõ são nulos, deixa minúsculo e tira os espaços em volta
            var initialCommands = commandText.Split(' ')
                                                        .Where(s=>!string.IsNullOrWhiteSpace(s))
                                                        .Select(s=>s.ToLower().Trim()).ToList();

            //Caso tenha algum caminho de arquivo padroniza pra um comando válido, caso seja válido
            initialCommands = NormalizeFilePaths(initialCommands);

            //Para cada comando
            for (int i = 0; i < initialCommands.Count; i++)
            {
                //Caso ele não tenha "-" só adiciona o comando
                var command = initialCommands[i];
                if (!command.Contains('-'))
                {
                    commands.Add(command);
                    continue;
                }

                //Verifica se o comando com hífen é valido
                if (!CommandHasMinusSymbolValid(command))
                    throw new InvalidOperationException($"comando '{initialCommands[i]}' inválido");

                try
                {
                    //Concatena ele e a próxima posição do array como um comando
                    commands.Add($"{initialCommands[i]} {initialCommands[i+1]}");
                    //Remove os dois
                    initialCommands.RemoveAt(i+1);
                }
                catch (Exception){ throw new InvalidOperationException($"comando '{initialCommands[i]}' inválido");}


            }

            return commands;
        }

        /// <summary>
        /// Padroniza os caminhos de arquivo
        /// </summary>
        /// <param name="commands">Comandos</param>
        /// <returns>Comandos normalizados</returns>
        private static List<string> NormalizeFilePaths(List<string> commands)
        {
            var normalizedCommands = new List<string>();
            string newCmd ="";
            string spaceBefore;
            bool containsDoubleQuoteAndSpaceBetween;

            for (int i = 0; i < commands.Count; i++)
            {
             
                containsDoubleQuoteAndSpaceBetween = commands[i].Count(c => c == '\"') == 1;

                //Se o comando tiver espaço e for um caminho: "Arquivo Teste.txt"
                if (containsDoubleQuoteAndSpaceBetween)
                {
                    //Vai concatenando as posições: [""a","e","b.txt"]-> "a e b.txt"
                    do
                    {
                        spaceBefore = string.IsNullOrWhiteSpace(newCmd) ? "" : " ";
                        newCmd += $"{spaceBefore}{commands[i]}";
                        i++;
                    } while (!commands[i].Contains('\"'));

                    newCmd += $" {commands[i]}";
                    normalizedCommands.Add(newCmd);
                    newCmd = "";
                }
                //Caso não só adiciona o comando
                else
                {
                    normalizedCommands.Add(commands[i]);
                }
                    
            }

            return normalizedCommands;
        }


        /// <summary>
        /// Verfica se o comando de símbolo é válido 
        /// </summary>
        /// <param name="command">texto do comando</param>
        /// <returns>true se for válido</returns>
        public static bool CommandHasMinusSymbolValid(string command)
        {
            var normalized = command.ToLower();
            var validCommands = new List<string>{ "-in", "-out", "-pontos", "-log" };
            return validCommands.Exists(c=>c==normalized);
        }
        /// <summary>
        /// Busca a prioridade do comando baseado no tipo
        /// </summary>
        /// <param name="type">Tipo do comando</param>
        /// <returns>A prioridade do comando</returns>
        public static CommandPriority GetCommandPriorityByType(CommandType type)
        {
            var priorities = new Dictionary<CommandType, CommandPriority>
            {
                { CommandType.Log, CommandPriority.NoNeed },
                { CommandType.ConsumersCount, CommandPriority.NoNeed },
                { CommandType.ChangeFileOutputName, CommandPriority.NoNeed },
                { CommandType.Update, CommandPriority.Standard },
                { CommandType.FileInputPath, CommandPriority.Standard },
                { CommandType.Simulate, CommandPriority.Critical },
                { CommandType.Totalize, CommandPriority.Critical },
                { CommandType.Finish, CommandPriority.Critical },
            };

            return priorities.First(p => p.Key == type).Value;
        }
    }
}
