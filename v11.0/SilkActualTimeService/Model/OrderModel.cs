using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class OrderModel
    {
        public string OrderNo { get; set; }

        public string BrandCode { get; set; }

        public string ProductLineCode { get; set; }

        public string BatchNo { get; set; }

        public string LineCode { get; set; }

        public string ProductCode { get; set; }

        public string StageCode { get; set; }


        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public string OrderState { get; set; }
    }
}
