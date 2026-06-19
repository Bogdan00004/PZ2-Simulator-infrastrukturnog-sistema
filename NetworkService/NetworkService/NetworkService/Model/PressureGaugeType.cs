using FontAwesome.WPF;
using System.Collections.Generic;
using System.Windows.Media;

namespace NetworkService.Model
{
    public class PressureGaugeType
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }  
        public FontAwesomeIcon Icon { get; set; }  
        public SolidColorBrush IconBrush { get; set; }

        public override string ToString() => Name;

        public static List<PressureGaugeType> PredefinedTypes => new List<PressureGaugeType>
        {
            new PressureGaugeType
            {
                Name      = "Cable Sensor",
                ImagePath = "/Resources/Images/CableSensor.png",
                Icon      = FontAwesomeIcon.Plug,
                IconBrush = new SolidColorBrush(Color.FromRgb(74, 127, 165))
            },
            new PressureGaugeType
            {
                Name      = "Digital Manometer",
                ImagePath = "/Resources/Images/DigitalManometer.png",
                Icon      = FontAwesomeIcon.Tachometer,
                IconBrush = new SolidColorBrush(Color.FromRgb(230, 168, 23))
            }
        };
    }
}