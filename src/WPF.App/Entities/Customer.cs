using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.App.Entities
{
    public class Customer
    {
        public string SelectedSeat { get; set; } //J07
        public string Sequence { get; set; } //CSP
        public DateTime SelectedSession { get; set; } //17:00
        public int ArrivalTime { get; set; }
        public OnUnavaibleSeatBehavior OnUnavaibleSeat { get; set; } //T - D
        public CustomerType CustomerType { get; set; } // R - M - C
        public int EstimatedTime { get; set; } //7


        public static CustomerType GetCustomerTypeFromIdentifier(string identifier)
        {
            if (identifier == null)
                throw new NullReferenceException("Identificador nulo.");

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

        public static OnUnavaibleSeatBehavior GetOnUnavaibleSeatBehaviorFromIdentifer(string identifier)
        {
            if (identifier == null)
                throw new NullReferenceException("Identificador nulo.");

            switch (identifier.ToLower())
            {

                case "t":
                    return OnUnavaibleSeatBehavior.TryAnother;
                default:
                    return OnUnavaibleSeatBehavior.GiveUp;

            }
        }
    }


    public enum CustomerType
    {
        Regular,
        HalfPrice,
        Premium
    }

    public enum OnUnavaibleSeatBehavior
    {
        TryAnother,
        GiveUp
    }


}
