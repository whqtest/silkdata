using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class ProcessRefTag
    {
        public string ProductLineCode { get; set; }

        public string LineCode { get; set; }

        public string ProductCode { get; set; }

        public string StageCode { get; set; }

        public string ProcessCode { get; set; }

        public string RefStartTag { get; set; }

        public string RefEndTag { get; set; }

        public string TagCode { get; set; }

        public string ParameterCode { get; set; }

        public string RefStartParamerterCode { get; set; }

        public string RefEndParameterCode { get; set; }

        public string ParameterType { get; set; }

        public string start_offset { get; set; }

        public string end_offset { get; set; }

        public string startvalue { get; set; }

        public string endvalue { get; set; }

        public string CutType { get; set; }
    }
}
