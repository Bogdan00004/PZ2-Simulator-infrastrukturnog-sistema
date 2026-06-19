using FontAwesome.WPF;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace NetworkService.Model
{
    public class PressureGaugeGroup
    {
        public string TypeName { get; set; }
        public string TypeImagePath { get; set; }
        public FontAwesomeIcon TypeIcon { get; set; }
        public SolidColorBrush TypeIconBrush { get; set; }
        public ObservableCollection<PressureGauge> Entities { get; set; } = new ObservableCollection<PressureGauge>();
    }
}