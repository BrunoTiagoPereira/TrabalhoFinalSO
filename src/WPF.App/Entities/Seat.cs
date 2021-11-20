using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
namespace WPF.App.Entities
{
    //Cadeira
    public class Seat : INotifyPropertyChanged
    {
        //Cor da cadeira
        public SolidColorBrush Color
        {
            get => _color;
            set
            {
                _color = value;
                //Ao alterar a cor , disparar evento de mudança de propriedades
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Color"));
            }
        }
        private SolidColorBrush _color;

        //Identificador
        public string Identifier { get; set; }

        //Parametro de dimensão do objeto na visualização
        public double Width { get; set; }
        public double Height { get; set; }

        //Parametros de posição do objeto na visualização
        public double Top { get; set; }
        public double Left { get; set; }
        
        //Disponibilidade
        public Status Status { get; set; }

        //Cliente que está utilizando a cadeira
        public Customer Customer { get; set; }

        //Evento de mudança de propriedades
        public event PropertyChangedEventHandler PropertyChanged;

    }

    public enum Status
    {
        Pending,
        Unavailable,
        Available
    }

}
