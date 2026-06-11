using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkService.Model
{
    public class EntityConnection
    {
        public int SlotIndexA { get; set; }
        public int SlotIndexB { get; set; }

        public string Key => SlotIndexA < SlotIndexB
            ? $"{SlotIndexA}-{SlotIndexB}"
            : $"{SlotIndexB}-{SlotIndexA}";
    }
}
