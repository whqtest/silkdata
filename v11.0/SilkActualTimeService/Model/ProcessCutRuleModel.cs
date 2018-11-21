using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class ProcessCutRuleModel
    {
        public string LineCode { get; set; }

        public string StageCode { get; set; }
        public string ProcessCode { get; set; }

        public string Flag { get; set; }

        public string ParameterCode { get; set; }



        public string Value { get; set; }

        public int AddTime { get; set; }
    }
}
