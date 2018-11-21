using SilkActualTimeService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.BLL_Service
{
    public class BLL_Log
    {
        public void CreateLog(OrderModel order,string text)
        {
            Batch_Log log = new Batch_Log();
            log.BatchNo = order.BatchNo;
            log.CreateBy = "sys";
            log.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            log.LineCode = order.LineCode;
            log.ProductCode = order.ProductCode;
            log.StageCode = order.StageCode;
            log.LogText = text;
            BLL_ZS_ActualTime.loglist.Add(log);
        }

        public void CreateStorageLog(StorageProcessModel model, string text)
        {
            Batch_Log log = new Batch_Log();
            log.LineCode = model.LineCode;
            log.StageCode = model.StageCode;
            log.ProcessCode = model.StorageCode;
            log.LogText = text;
            BLL_ZS_Storage.loglist.Add(log);
        }
    }
}
