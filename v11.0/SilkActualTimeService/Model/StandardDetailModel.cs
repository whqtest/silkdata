using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class StandardDetailModel
    {
        public string VersionNo { get; set; }

        public string VersionID { get; set; }

        public string LineCode { get; set; }

        public string ProductCode { get; set; }

        public string StageCode { get; set; }

        public string ProcessCode { get; set; }

        public string ParameterCode { get; set; }

        public string UpValue { get; set; }

        public bool IsUp { get; set; }

        public string DownValue { get; set; }

        public bool IsDown { get; set; }

        public string CenterValue { get; set; }
    }
}
