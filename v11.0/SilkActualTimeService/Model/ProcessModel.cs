using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class ProcessModel
    {
        public string ProductLineCode { get; set; }

        public string LineCode { get; set; }

        public string LineName { get; set; }

        public string ProductCode { get; set; }

        public string StageCode { get; set; }

        public string StageName { get; set; }

        public string ProcessCode { get; set; }

        public int SortNo { get; set; }
    }
}
