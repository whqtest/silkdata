using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class BatchModel
    {
        public string BatchCode { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public string ProductLineCode { get; set; }

        public string BatchState { get; set; }
    }
}
