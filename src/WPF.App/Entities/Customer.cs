using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.App.Entities
{
    //Cliente
    public class Customer
    {
        //Assento selecionado
        public string SelectedSeat { get; set; } //J07

        //Sequencia de ações
        public string Sequence { get; set; } //CSP

        //Sessão selecionada
        public DateTime SelectedSession { get; set; } //17:00

        //Hora de chegada
        public int ArrivalTime { get; set; }

        //Disponibilidade do assento
        public OnUnavailableSeatBehavior OnUnavailableSeat { get; set; } //T - D

        //Tipo de cliente
        public CustomerType CustomerType { get; set; } // R - M - C

        //Tempo estimado
        public int EstimatedTime { get; set; } //7


        //Método para identificar o tipo de cliente 
        public static CustomerType GetCustomerTypeFromIdentifier(string identifier)
        {
            //verifica se o identificador é nulo e , caso sim, retorna uma exceção
            if (identifier == null)
                throw new NullReferenceException("Identificador nulo.");

            //Switch para identificar qual o tipo do cliente a partir do identficador
            switch (identifier.ToLower())
            {
                case "m":
                    return CustomerType.HalfPrice;
                case "c":
                    return CustomerType.Premium;
                default:
                    return CustomerType.Regular;
            }

        }

        //Método para identificar a disponibilidade do assento
        public static OnUnavailableSeatBehavior GetOnUnavailableSeatBehaviorFromIdentifier(string identifier)
        {
            //verifica se o identificador é nulo e , caso sim, retorna uma exceção
            if (identifier == null)
                throw new NullReferenceException("Identificador nulo.");

            //Switch para identificar qual a disponibilidade do assento a partir do identficador
            switch (identifier.ToLower())
            {
                case "t":
                    return OnUnavailableSeatBehavior.TryAnother;
                default:
                    return OnUnavailableSeatBehavior.GiveUp;
            }

        }

    }

    //Enumerado de tipo de cliente
    public enum CustomerType
    {
        Regular,
        HalfPrice,
        Premium
    }

    //Enumerado de comportamentos do cliente se o assento estiver indisponivel
    public enum OnUnavailableSeatBehavior
    {
        TryAnother,
        GiveUp
    }


}
