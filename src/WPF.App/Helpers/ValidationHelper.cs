using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WPF.App.Entities;
using WPF.App.Public;

namespace WPF.App.Helpers
{
    public static class ValidationHelper
    {
        //Regexes para validações
        private static Regex _customerValidation => new Regex(@"[A-z]\d{1,2};[0-2]\d{1}:[0-5]\d{1};(C|X)(S|X)(P|X);(T|D);(R|M|C);\d{1,};\d{1,}");
        private static Regex _roomConfigurationSize => new Regex(@"^[1-9]\d{0,}x[1-9]\d{0,}\s{0,}$");
        //Not perfect but ok
        private static Regex _roomConfigurationSessions => new Regex(@"([0-2]\d{1}:[0-5]\d{1},?)+");

        //Dicionário de identificação de comandos e os tipos
        private static Dictionary<Regex, CommandType> _commandsValidations = new Dictionary<Regex, CommandType>
        {
            { new Regex(@"^simular$", RegexOptions.IgnoreCase), CommandType.Simulate },
            { new Regex(@"^alterar", RegexOptions.IgnoreCase), CommandType.Update },
            { new Regex(@"^-log\s{0,}(arquivo|tela)?$", RegexOptions.IgnoreCase), CommandType.Log },
            { new Regex(@"^-pontos\s{0,}[1-9]\d{0,}$", RegexOptions.IgnoreCase), CommandType.ConsumersCount },
            { new Regex(@"^-in\s{0,}\"".{1,}\.txt\""$", RegexOptions.IgnoreCase), CommandType.FileInputPath },
            { new Regex(@"^-out\s{0,}\"".{1,}\.txt\""$", RegexOptions.IgnoreCase), CommandType.ChangeFileOutputName },
            { new Regex(@"^totalizar$", RegexOptions.IgnoreCase), CommandType.Totalize },
            { new Regex(@"^finalizar$", RegexOptions.IgnoreCase), CommandType.Finish },
        };
       
        //Valida o Cliente informado e retorna o resultado da validação
        public static bool ValidateCustomer(string lineInfo)
        {
            return lineInfo!=null && _customerValidation.IsMatch(lineInfo);
        }

        //Valida a Sala/Sessão informado e retorna o resultado da validação
        public static bool ValidateRoomConfig(List<string> roomConfiguration)
        {
            if (roomConfiguration == null)
                return false;

            if (roomConfiguration.Count != 2)
                return false;

            if (!_roomConfigurationSize.IsMatch(roomConfiguration[0]))
                return false;

            if (!_roomConfigurationSessions.IsMatch(roomConfiguration[1]))
                return false;

            return true;
        }

        #region DevTools
        /// <summary>
        /// Valida os comandos do devTools
        /// </summary>
        /// <param name="commandText">Texto dos comandos</param>
        /// <param name="commands">Lista de comandos</param>
        /// <returns></returns>
        public static ValidationResult ValidateDevToolsCommand(string commandText, out List<Command> commands)
        {

            var result = new ValidationResult();
            var validation = new ValidationResult();
            var rawCommands = new List<string>();
            var normalizedCommands = new List<Command>();
            commands = new List<Command>();

            //Faz a validação e verifica se tem erros para retornar

            //Verifica se há algum comando nulo
            if ((validation = IsCommandNull(commandText)).HasErrors)
            {
                commands = normalizedCommands;
                result.Errors.AddRange(validation.Errors);
                return result;
            }

            //Verifica a sintáxe dos comandos se é valida
            if ((validation = IsCommandSyntaxValid(commandText, out rawCommands)).HasErrors)
            {
                commands = normalizedCommands;
                result.Errors.AddRange(validation.Errors);
                return result;
            }


            //Verifica se os comandos são válidos
            if ((validation = AreCommandsValid(rawCommands, out normalizedCommands)).HasErrors)
            {
                result.Errors.AddRange(validation.Errors);
                return result;
            }

            //Verifica se os comandos são válidos no contexto
            if ((validation = AreCommandsValidInContext(normalizedCommands)).HasErrors)
            {
                result.Errors.AddRange(validation.Errors);
                return result;
            }
            commands = normalizedCommands;
            return result;
        }

        /// <summary>
        /// Verifica se os comandos são válidos no contexto
        /// </summary>
        /// <param name="commands">Comandos</param>
        /// <returns></returns>
        private static ValidationResult AreCommandsValidInContext(List<Command> commands)
        {
            var validation = new ValidationResult();

            var addFileValid = !commands.Any(c => c.Type == CommandType.Update) || commands.Any(c => c.Type == CommandType.FileInputPath);

            if (!addFileValid)
            {
                validation.Errors.Add(new ValidationError
                {
                    Error = "Comandos para adicionar arquivo devem ser seguidos de comandos '-in {nome_arquivo}'"
                });
            }

            var hasInputFileCommands = commands.Any(c => c.Type == CommandType.FileInputPath);
            var hasUpdateCommand = commands.Any(c => c.Type == CommandType.Update);
            var inputFileValid = !hasInputFileCommands || (hasUpdateCommand && hasUpdateCommand);

            if (!inputFileValid)
            {
                validation.Errors.Add(new ValidationError
                {
                    Error = "Comandos para inserir arquivos necessitam de um comando 'alterar' no texto do comando"
                });
            }
            return validation;


        }

        /// <summary>
        /// Verifica se os comandos são válidos
        /// </summary>
        /// <param name="rawCommands">textos dos comandos</param>
        /// <param name="commands">lista de objetos de comandos</param>
        /// <returns></returns>
        private static ValidationResult AreCommandsValid(List<string> rawCommands, out List<Command> commands)
        {

            var validation = new ValidationResult();
            commands = new List<Command>();
            foreach (var rawCmd in rawCommands)
            {
                var matched = _commandsValidations.FirstOrDefault(x => x.Key.IsMatch(rawCmd));
                if (matched.Key == null)
                {
                    validation.Errors.Add(new ValidationError()
                    {
                        Error = $"comando '{rawCmd}' não é válido"
                    });
                    continue;
                }

                commands.Add(new Command()
                {
                    Text = rawCmd,
                    Type = matched.Value
                });

            }

            return validation;
        }

        /// <summary>
        /// Verifica se a sintaxe de comandos é valida
        /// </summary>
        /// <param name="commandText">Texto do comando</param>
        /// <param name="rawCommands">Comandos separados</param>
        /// <returns></returns>
        private static ValidationResult IsCommandSyntaxValid(string commandText, out List<string> rawCommands)
        {
            var validation = new ValidationResult();
            rawCommands = new List<string>();

            if (!HasValidPath(commandText))
            {
                validation.Errors.Add(new ValidationError()
                {
                    Error = $"O comando '{commandText}' tem caminho de arquivos inválidos, especifique um caminho sempre com aspas - \"ArquivoExemplo.txt\""
                });
                return validation;
            }


            try
            {
                rawCommands = Util.GetNormalizedCommands(commandText);
            }
            catch (InvalidOperationException e)
            {

                validation.Errors.Add(new ValidationError()
                {
                    Error = e.Message
                });

            }
            return validation;
        }


        /// <summary>
        /// Verifica se o comando tem caminho válido (aspas fechando) 
        /// </summary>
        /// <param name="commandText">texto do comando</param>
        /// <returns>true se sim</returns>
        private static bool HasValidPath(string commandText)
        {
            return commandText.Count(c => c == '\"') % 2 == 0;
        }

        /// <summary>
        /// Verifica se o comando é nulo
        /// </summary>
        /// <param name="commandText">Texto do comando</param>
        /// <returns></returns>
        private static ValidationResult IsCommandNull(string commandText)
        {
            var validation = new ValidationResult();
            //Verifica se o comando é vazio
            if (string.IsNullOrWhiteSpace(commandText))
            {
                validation.Errors.Add(new ValidationError()
                {
                    Error = "O comando não pode ser vazio",

                });

            }
            return validation;
        }



        #endregion


    }

}
