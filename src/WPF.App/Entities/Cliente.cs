using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.App.Entities
{
    public class Cliente
    {
        public string Assento { get; set; } //J07
        public string Comportamento { get; set; } //CSP
        public DateTime SessaoEscolhida { get; set; } //17:00
        public EstadoPoltrona EstadoPoltrona { get; set; } //T - D
        public TipoCliente TipoCliente { get; set; } // R - M - C

        public int TempoEstimado { get; set; } //7
    }

    public enum TipoCliente
    {
        Regular,
        MeiaEntrada,
        ClubeCinema
    }

    public enum EstadoPoltrona
    {
        TentaOutra,
        Desiste
    }


}
