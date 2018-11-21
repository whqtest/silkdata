using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class Batch_Log
    {
        public string BatchNo { get; set; }

        public string ProductLineCode { get; set; }

        public string LineCode { get; set; }

        public string ProductCode { get; set; }

        public string StageCode { get; set; }

        public string ProcessCode { get; set; }

        public string LogText { get; set; }

        public string CreateTime { get; set; }

        public string CreateBy { get; set; }
    }
}
