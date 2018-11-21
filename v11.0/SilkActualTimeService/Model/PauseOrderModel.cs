using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class PauseOrderModel
    {
        public string OrderNo { get; set; }

        public string BatchNo { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public int SortNo { get; set; }
    }
}
