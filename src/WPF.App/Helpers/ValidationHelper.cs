using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WPF.App.Helpers
{
    public static class ValidationHelper
    {

        private static Regex _customerValidation => new Regex(@"[A-z]\d{1,2};[0-1]\d{1}:[0-5]\d{1};(C|X)(S|X)(P|X);(T|D);(R|M|C);\d{1}");
        private static Regex _roomConfigurationSize => new Regex(@"^[1-9]\d{0,}x[1-9]\d{0,}$");

        //Not perfect but ok
        private static Regex _roomConfigurationSessions => new Regex(@"([0-1]\d{1}:[0-5]\d{1},?)+");

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


    }

}
