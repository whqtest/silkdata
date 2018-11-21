using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class MeterielConfig
    {
        public string LineCode { get; set; }

        public string StageCode { get; set; }

        public string ProductCode { get; set; }

        /// <summary>
        /// 工单开始时间参考点
        /// </summary>
        public string StartParameterCode { get; set; }

        /// <summary>
        /// 工单结束时间参考点
        /// </summary>
        public string EndParameterCode { get; set; }

        /// <summary>
        /// 掺配类型
        /// </summary>
        public string CPType { get; set; }

        /// <summary>
        /// 掺配累计量点
        /// </summary>
        public string ParameterCode { get; set; }

        /// <summary>
        /// 工单投入累计量点
        /// </summary>
        public string InputCode { get; set; }

        /// <summary>
        /// 工单产出累计量点
        /// </summary>
        public string OutputCode { get; set; }
    }
}
