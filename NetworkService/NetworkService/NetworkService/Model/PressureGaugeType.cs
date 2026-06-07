using System.Collections.Generic;

namespace NetworkService.Model
{
    public class PressureGaugeType
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }

        public override string ToString() => Name;

        // Predefined types for T1
        public static List<PressureGaugeType> PredefinedTypes => new List<PressureGaugeType>
        {
            new PressureGaugeType
            {
                Name      = "Cable Sensor",
                ImagePath = "/Resources/Images/CableSensor.png"
            },
            new PressureGaugeType
            {
                Name      = "Digital Manometer",
                ImagePath = "/Resources/Images/DigitalManometer.png"
            }
        };
    }
}