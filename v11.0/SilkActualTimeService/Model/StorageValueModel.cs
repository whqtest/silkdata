using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class StorageValueModel
    {
        public string BatchNo { get; set; }

        public string OrderNo { get; set; }

        public string BrandCode { get; set; }

        public string LineCode { get; set; }

        public string StageCode { get; set; }

        public string StorageType { get; set; }

        public string StorageCode { get; set; }
        public string StorageName { get; set; }

        public string InOutFlag { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Value { get; set; }

        public string SYValue { get; set; }

        public StorageValueModel(StorageProcessModel p, string batchno, string orderno,string brandcode, string starttime, string endtime, string value,string syvalue, string flag)
        {
            this.OrderNo = orderno;
            this.BatchNo = batchno;
            this.StartTime = starttime;
            this.EndTime = endtime;
            this.InOutFlag = flag;
            this.LineCode = p.LineCode;
            this.StageCode = p.StageCode;
            this.StorageCode = p.StorageCode;
            this.StorageName = p.StorageName;
            this.StorageType = p.StorageType;
            this.Value = value;
            this.SYValue = syvalue;
            this.BrandCode = brandcode;
        }
    }

  
}
