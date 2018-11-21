using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class ContinueBatchModel
    {
        public string BatchNo { get; set; }

        public string ProductLineCode { get; set; }

        public string LineCode { get; set; }

        public string ProductCode { get; set; }

        public string StageCode { get; set; }

        public string IUID { get; set; }


        public int SortNo { get; set; }
    }
}
