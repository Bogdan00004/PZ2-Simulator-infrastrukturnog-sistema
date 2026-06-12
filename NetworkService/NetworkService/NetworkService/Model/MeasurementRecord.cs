using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkService.Model
{
    public class MeasurementRecord
    {
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsValid { get; set; }
    }
}