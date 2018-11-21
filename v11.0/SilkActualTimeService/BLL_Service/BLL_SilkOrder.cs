using SilkActualTimeService.DAL_GetData;
using SilkActualTimeService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.BLL_Service
{
    public class BLL_SilkOrder
    {
        public static Queue<OrderModel> OrderQueue = new Queue<OrderModel>();
        DAL_Data dal = new DAL_Data();
        private object _orderkey = new object();
        public void WatchOrder(string linecode)
        {
            IList<OrderModel> orderlist = dal.GetOrderList(linecode);//获取未归集工单
            lock (_orderkey)
            {
                foreach (OrderModel o in orderlist)
                {
                    if (!OrderQueue.Contains(o))
                    {
                        OrderQueue.Enqueue(o);
                        dal.UpdateOrder(o.OrderNo,"1");
                    }
                }
            }
        }

        public void CorrectProduct(OrderModel order)
        {

        }

    }

}
