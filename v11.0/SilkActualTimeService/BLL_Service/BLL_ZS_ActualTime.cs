using SilkActualTimeService.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SilkActualTimeService.DAL_GetData;
using SilkActualTimeService.BLL_Service;
using System.Configuration;

namespace SilkActualTimeService.BLL_Service
{
    public class BLL_ZS_ActualTime
    {
        private object _orderkey = new object();
        IList<PauseOrderModel> PauseOrderlist = new List<PauseOrderModel>();//工单暂停记录
        IList<ProcessMainTag> ProcessMaintaglist = new List<ProcessMainTag>();//工序主要参数点表
        IList<ProcessRefTag> ProcessRefTaglist = new List<ProcessRefTag>();//工序参考参数点
        IList<ProcessCutRuleModel> Cutlist = new List<ProcessCutRuleModel>();//截取规则列表
        IList<ProcessModel> processlist = new List<ProcessModel>();//获取工艺建模数据-到工序
        IList<ParameterModel> parameterlist = new List<ParameterModel>();//获取工艺建模数据-到参数
        IList<OrderShutDownModel> shutlist = new List<OrderShutDownModel>();//存储工单的停机断料记录
        public static IList<Batch_Log> loglist = new List<Batch_Log>();
        BLL_Log logbll = new BLL_Log();
        DAL_Data dal = new DAL_Data();
        BLL_CreateTable tablebll = new BLL_CreateTable();
        int cyclic = Convert.ToInt32(ConfigurationManager.AppSettings["cyclic"].ToString());

        public BLL_ZS_ActualTime()
        {
            //初始化所有数据
            ProcessMaintaglist = dal.GetMainParameterlist();
            ProcessRefTaglist = dal.GetRefParameterlist();
            Cutlist = dal.GetCutRulelist();
            processlist = dal.GetProcessList();
            parameterlist = dal.GetParameterlist();
        }

        public string Start(string linecode)
        {
            OrderModel order1 = new OrderModel();
            try
            {
                string linename = "";
                if (linecode == "GYLX_YX")
                    linename = "叶线";
                if (linecode == "GYLX_GX")
                    linename = "梗线";
                try
                {
                    IList<OrderModel> orderlist = dal.GetOrderList(linecode);
                    if (orderlist.Count > 0)
                    {
                        OrderModel order = orderlist[0];
                        order1 = order;
                        bool flag = CheckData(order);
                        if (flag)
                        {
                            bool ff = CheckTime(order, order.EndTime);
                            if (ff)
                            {
                                Cut(order);
                            }
                            else
                            {
                                shutlist.Clear();
                                loglist.Clear();
                                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":" + linename + "批次号为:" + order.BatchNo + " 工单号为：" + order.OrderNo + "正在等待工单结束！\n";
                            }
                        }
                        else
                        {
                            dal.UpdateOrder(orderlist[0].OrderNo, "3");
                            logbll.CreateLog(order1, "校验数据不通过，请检查");
                        }
                        DataTable logdt = tablebll.CreateLogtable();
                        tablebll.FillLogTable(loglist, ref logdt);
                        dal.InsertTable(logdt);//插入日志表
                        loglist.Clear();
                        shutlist.Clear();
                        string dtnow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        return dtnow + ":" + linename + "批次号为:" + order.BatchNo + " 工单号为：" + order.OrderNo + "计算完成！\n";
                    }
                    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":" + linename + "当前无需要的工单！\n";
                }
                catch (Exception er)
                {
                    logbll.CreateLog(order1, "报错" + er.Message);
                    dal.UpdateOrder(order1.OrderNo, "3");
                    DataTable logdt = tablebll.CreateLogtable();
                    tablebll.FillLogTable(loglist, ref logdt);
                    dal.InsertTable(logdt);//插入日志表
                    loglist.Clear();
                    shutlist.Clear();
                    return "报错，请检查:" + er.Message;
                }
            }
            catch (Exception e)
            {
                loglist.Clear();
                shutlist.Clear();
                return "报错，请检查:" + e.Message;
            }
        }

        /// <summary>
        /// 截取工单时间
        /// </summary>
        /// <param name="order">工单信息</param>
        public void Cut(OrderModel order)
        {
            string checktype = ConfigurationManager.AppSettings["checktype"].ToString();//读取配置文件
            IList<ParameterTimeModel> parametertimelist = new List<ParameterTimeModel>();//用来存储该工单下面的所有参数点的稳态开始时间、稳态结束时间、完整开始时间、完整结束时间
            //获取该工单是否存在暂停记录
            IList<PauseOrderModel> pauselist = new List<PauseOrderModel>();
            //获取连批开的批次
            IList<ContinueBatchModel> continuelist = dal.GetContinuebatchlist();
            //对工单的开始时间和结束时间不进行扩展
            IList<ProcessModel> temppr = processlist.Where(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode).ToList();
            IList<CPConfigModel> configlist = dal.GetConfigList(order.StageCode, order.BrandCode);
            IList<InOutConfigModel> inoutlist = dal.GetInoutlist(order.StageCode, order.BrandCode);
            IList<LJLJugeModel> jugelist = new List<LJLJugeModel>();//存放电子秤行走路线
            string pr = "";
            foreach (ProcessModel p in temppr)
            {
                //获取主要参数点
                IList<ProcessMainTag> maintaglist = ProcessMaintaglist.Where(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode && j.ProcessCode == p.ProcessCode).ToList();
                //主要参数点截取
                if (continuelist.Count(j => j.BatchNo == order.BatchNo && order.LineCode == j.LineCode && j.StageCode == order.StageCode) > 0)
                    ContinueCut(p.ProcessCode, order, maintaglist, false, continuelist, ref parametertimelist);
                else
                    DontContinueCut(p.ProcessCode, order, maintaglist, false, ref parametertimelist, ref pr);
                //参考参数点截取
                //RefParameterCut(p.ProcessCode, order, ref parametertimelist);
                RefNewParameterCut(p.ProcessCode, order, ref parametertimelist, cyclic);
            }
            //所有工序时间已归集完成，开始计算
            string updatesql = "";
            Hashtable ha = new Hashtable();
            DataTable spchistory = tablebll.CreateSPCtable();
            Computer(parametertimelist, order, checktype, cyclic, ref spchistory, ref updatesql, ProcessRefTaglist, continuelist, pr);
            int del = dal.DeleteSPC(order.BatchNo, order.ProductCode);
            int result = 0;
            if (del == -1)
                logbll.CreateLog(order, "删除SPC信息失败，请检查！");
            else
            {
                result = dal.InsertTable(spchistory);//插入SPC表
                if (result > 0)
                {
                    logbll.CreateLog(order, "插入SPC_HISTORY成功！");
                    #region 调试时请注释
                    string r = CheckSQL(order, updatesql);
                    if (string.IsNullOrEmpty(r))
                        logbll.CreateLog(order, "更新制丝过程检验失败！");
                    else
                    {
                        result = dal.UpdateCheck(updatesql);//更新制丝过程检验结果表
                        if (result > 0)
                        {
                            string checktypeid = ConfigurationManager.AppSettings["checktype"].ToString();
                            DataTable zsqurqtable = dal.GetCheck_ZSQURQ(checktypeid, order.BatchNo);
                            string zsqurqid = "";
                            if (zsqurqtable.Rows.Count > 0)
                                zsqurqid = zsqurqtable.Rows[0]["ZSQURQID"].ToString();
                            int rr = dal.Updatezsqurqstate(zsqurqid);
                            if (rr > 0)
                                logbll.CreateLog(order, "更新制丝过程检验请求状态成功！");
                            else
                                logbll.CreateLog(order, "更新制丝过程检验请求状态失败！");
                            DataTable zsqucktable = dal.GetCheckZSQUCK(zsqurqid, order.ProductCode);
                            string zsquckid = "";
                            if (zsqucktable.Rows.Count > 0)
                                zsquckid = zsqucktable.Rows[0]["ZSQUCKID"].ToString();
                            int rr1 = dal.Updatezsquckstate(zsquckid);
                            if (rr1 > 0)
                                logbll.CreateLog(order, "更新制丝过程检验工段样本状态成功！");
                            else
                                logbll.CreateLog(order, "更新制丝过程检验工段样本状态失败！");
                            logbll.CreateLog(order, "更新制丝过程检验参数值成功！");
                        }
                        else
                            logbll.CreateLog(order, "更新制丝过程检验参数值失败！");
                    }
                    MaterielConsume(order, configlist, ProcessRefTaglist, inoutlist, parameterlist, continuelist, ref ha);//更新工单时间信息，投入产出、掺配信息
                    //这里需对掺配称的信息在spc表中进行删除
                    string tempsql = tablebll.GetDeleteSPC(order, ha, parameterlist);
                    if (tempsql != "99999" && !string.IsNullOrEmpty(tempsql))
                        del = dal.DeleteStorage(tempsql);
                    //插入断料信息
                    DataTable shuttable = tablebll.CreateShutDowntable();
                    tablebll.FillBatchShutTable(shutlist, ref shuttable);
                    del = dal.DeleteShut(order.BatchNo, order.OrderNo);
                    if (del == -1)
                        logbll.CreateLog(order, "删除断料信息失败，请检查！");
                    else
                        result = dal.InsertTable(shuttable);
                    #endregion
                }
                else
                {
                    logbll.CreateLog(order, "插入SPC_HISTORY失败！");
                }
            }
            if (order.StageCode == "GYD_JX" || order.StageCode == "GYD_YSB")
            {
                string csl = ComputerCSL(order);
                dal.Updatebatchcsl(order.BatchNo, csl);
            }
            dal.UpdateOrder(order.OrderNo, "3");
            DataTable logdt = tablebll.CreateLogtable();
            tablebll.FillLogTable(loglist, ref logdt);
            result = dal.InsertTable(logdt);//插入日志表
            loglist.Clear();
            shutlist.Clear();
        }



        /// <summary>
        /// 计算指标
        /// </summary>
        /// <param name="parametertimelist">参数归集时间信息列表</param>
        /// <param name="order">工单信息</param>
        /// <param name="checktype">检验标准类型</param>
        /// <param name="cyclic">取样周期</param>
        /// <param name="spchistory">SPC归集原始表</param>
        /// <param name="sqlstring">更新过程检验结果信息SQL</param>
        public void Computer(IList<ParameterTimeModel> parametertimelist, OrderModel order, string checktype, int cyclic, ref DataTable spchistory, ref string sqlstring, IList<ProcessRefTag> reflist, IList<ContinueBatchModel> continuelist, string pr)
        {
            string jugejl = ConfigurationManager.AppSettings["jugejl"].ToString();
            IList<StandardDetailModel> standardlist = GetStandardList(order, checktype);
            string productcode = "";
            IList<CheckResultModel> list = new List<CheckResultModel>();
            foreach (ParameterTimeModel p in parametertimelist)
            {
                if (jugejl.Contains(pr) && p.ProcessCode == pr)
                {
                    continue;
                }
                bool sflag = true;
                DataRow dr = spchistory.NewRow();
                if (standardlist.Count(j => j.ProcessCode == p.ProcessCode && j.ParameterCode == p.ParameterCode) == 0)
                {
                    logbll.CreateLog(order, p.ParameterCode + "参数不存在工艺标准，请检查！");
                    sflag = false;
                }
                CheckResultModel checkmodel = new CheckResultModel();
                checkmodel.ProductLineCode = p.ProductLineCode;
                checkmodel.ProductCode = p.ProductCode;
                checkmodel.LineCode = p.LineCode;
                checkmodel.StageCode = p.StageCode;
                checkmodel.ProcessCode = p.ProcessCode;
                checkmodel.ParameterCode = p.ParameterCode;
                checkmodel.HisTag = p.HisTag;
                ArrayList array = new ArrayList();
                if (string.IsNullOrEmpty(productcode))
                    productcode = p.ProductCode;
                string year = "20" + order.BatchNo.Substring(0, 2);
                string[] result = new string[18];
                StandardDetailModel model = new StandardDetailModel();
                if (sflag)
                {
                    model = standardlist.Where(j => j.ProcessCode == p.ProcessCode && j.ParameterCode == p.ParameterCode).ToList()[0];
                }
                string starttime = p.SteadyStartTime;
                string endtime = p.SteadyEndTime;
                if (string.IsNullOrEmpty(starttime))
                    starttime = p.StartTime;
                if (string.IsNullOrEmpty(endtime))
                    endtime = p.EndTime;
                string tag = p.HisTag;
                DataTable dt = new DataTable();
                if (!string.IsNullOrEmpty(starttime) && !string.IsNullOrEmpty(endtime))
                    dt = dal.GetHisData(starttime, endtime, tag, cyclic);
                try
                {
                    string up = "0";
                    string down = "0";
                    string center = "0";
                    if (sflag)
                    {
                        up = string.IsNullOrEmpty(model.UpValue) ? "0" : model.UpValue;
                        center = string.IsNullOrEmpty(model.CenterValue) ? "0" : model.CenterValue;
                        down = string.IsNullOrEmpty(model.DownValue) ? "0" : model.DownValue;
                    }
                    KPI kpi = new KPI(ConvertToArray(dt), Convert.ToSingle(up), Convert.ToSingle(down), Convert.ToSingle(center));
                    string type = "";
                    if (reflist.Count(j => j.ParameterCode == p.ParameterCode) > 0)
                    {
                        type = reflist.Where(j => j.ParameterCode == p.ParameterCode).ToList()[0].ParameterType;
                    }
                    float max = kpi.MAX();
                    if (type == "LJL")
                    {
                        max = float.Parse(ContinueLJL(p.ParameterCode, p.HisTag, max.ToString(), order, continuelist, cyclic));
                    }
                    float min = kpi.MIN();
                    int allpoint = kpi.AllPoint();
                    float avg = kpi.AVG();
                    float std = kpi.STD();
                    float cpk = kpi.CPK();
                    float cp = kpi.CP();
                    float cv = kpi.CV();
                    float pass = kpi.PASS();
                    int passpoint = kpi.PassPoint();
                    int uslpoint = kpi.UslPoint();
                    int lslpoint = kpi.LslPoint();
                    float percentofpass = kpi.PercentOfPass();
                    result[0] = up; array.Add(up);
                    result[1] = down; array.Add(down);
                    result[2] = center; array.Add(center);
                    result[3] = avg.ToString(); array.Add(avg.ToString());
                    result[4] = max.ToString(); array.Add(max.ToString());
                    result[5] = min.ToString(); array.Add(min.ToString());
                    result[6] = cv.ToString(); array.Add(cv.ToString());//std1
                    result[7] = std.ToString(); array.Add(std.ToString());
                    result[8] = cp.ToString(); array.Add(cp.ToString());//cpk1
                    result[9] = cpk.ToString(); array.Add(cpk.ToString());
                    result[10] = allpoint.ToString(); array.Add(allpoint.ToString());
                    result[11] = uslpoint.ToString(); array.Add(uslpoint.ToString());
                    result[12] = lslpoint.ToString(); array.Add(lslpoint.ToString());
                    result[13] = pass.ToString(); array.Add(pass.ToString());
                    result[14] = "0"; array.Add("0");
                    result[15] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    result[16] = "0"; array.Add("0");
                    result[17] = "0"; array.Add("0");
                    checkmodel.array = array;
                    list.Add(checkmodel);
                    tablebll.FillSPCTable("", order.BrandCode, order.BatchNo, "", year, p.ProductCode, p.ParameterCode, p.StartTime, p.EndTime, p.SteadyStartTime, p.SteadyEndTime, result, ref spchistory);
                }
                catch (Exception e)
                {
                    logbll.CreateLog(order, p.ParameterCode + ":计算指标时" + e.Message);
                }

            }
            sqlstring = tablebll.GetCheckUpdateSql(checktype, order.BatchNo, productcode, list, parametertimelist);
        }

        /// <summary>
        /// 检验SQL语句
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <param name="sqlstring">SQL语句</param>
        /// <returns></returns>
        public string CheckSQL(OrderModel order, string sqlstring)
        {
            string r = "";
            switch (sqlstring)
            {
                case "-1":
                    logbll.CreateLog(order, "检验样本主表中不存在数据，请检查！");
                    break;
                case "-2":
                    logbll.CreateLog(order, "工单检验样本表中不存在数据，请检查！");
                    break;
                case "-3":
                    logbll.CreateLog(order, "参数检验样本表中参数编码为空，请检查！");
                    break;
                case "-4":
                    logbll.CreateLog(order, "参数检验样本表中不存在数据，请检查！");
                    break;
                default:
                    r = sqlstring;
                    break;
            }
            return r;
        }

        /// <summary>
        /// 将原始数据转换数组
        /// </summary>
        /// <param name="dt">原始数据</param>
        /// <returns></returns>
        private float[] ConvertToArray(DataTable dt)
        {
            float[] f = new float[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                f[i] = Convert.ToSingle(dt.Rows[i]["vValue"].ToString());
            }
            return f;
        }

        /// <summary>
        /// 参考参数点截取
        /// </summary>
        /// <param name="order">工单</param>
        /// <param name="timelist">参数时间列表</param>
        public void RefParameterCut(string processcode, OrderModel order, ref IList<ParameterTimeModel> timelist)
        {
            int refstartadd = Convert.ToInt32(ConfigurationManager.AppSettings["refallstartadd"].ToString());
            int refendadd = Convert.ToInt32(ConfigurationManager.AppSettings["refallendadd"].ToString());
            int refsteadystartadd = Convert.ToInt32(ConfigurationManager.AppSettings["refsteadystartadd"].ToString());
            int refsteadyendadd = Convert.ToInt32(ConfigurationManager.AppSettings["refsteadyendadd"].ToString());
            if (ProcessRefTaglist.Count(j => j.LineCode == order.LineCode && j.ProcessCode == processcode) == 0)
                return;
            IList<ProcessRefTag> templist = ProcessRefTaglist.Where(j => j.LineCode == order.LineCode && j.ProcessCode == processcode).ToList();
            int ljl = Convert.ToInt32(ConfigurationManager.AppSettings["ljladd"].ToString());
            foreach (ProcessRefTag p in templist)
            {
                string starttime = "";
                string endtime = "";
                string steadystarttime = "";
                string steadyendtime = "";
                refstartadd = string.IsNullOrEmpty(p.start_offset) ? 0 : Convert.ToInt32(p.start_offset);
                refsteadystartadd = refstartadd;
                refendadd = string.IsNullOrEmpty(p.end_offset) ? 0 : Convert.ToInt32(p.end_offset);
                refsteadyendadd = refendadd;
                if (timelist.Count(j => j.ParameterCode == p.RefStartParamerterCode) > 0)
                {
                    ParameterTimeModel model = timelist.Where(j => j.ParameterCode == p.RefStartParamerterCode).ToList()[0];
                    if (string.IsNullOrEmpty(model.StartTime) || string.IsNullOrEmpty(model.SteadyStartTime))
                        continue;
                    if (p.ParameterType == "LJL")
                    {
                        starttime = model.StartTime;
                        steadystarttime = model.SteadyStartTime;
                    }
                    else if (p.ParameterType == "CLWD")
                    {
                        starttime = model.StartTime;
                        steadystarttime = model.SteadyStartTime;
                    }
                    else
                    {
                        starttime = Convert.ToDateTime(model.StartTime).AddSeconds(refstartadd).ToString("yyyy-MM-dd HH:mm:ss");
                        steadystarttime = Convert.ToDateTime(model.SteadyStartTime).AddSeconds(refsteadystartadd).ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
                else
                {
                    //查询数据库
                    IList<ParameterTimeModel> list = dal.GetParameterTimelist(order.BatchNo, p.RefStartParamerterCode);
                    if (list.Count == 0)
                    {
                        logbll.CreateLog(order, p.ParameterCode + "查找不到开始参考点，请检查！");
                    }
                    else
                    {
                        ParameterTimeModel model = list.Where(j => j.ParameterCode == p.RefStartParamerterCode).ToList()[0];
                        if (string.IsNullOrEmpty(model.StartTime) || string.IsNullOrEmpty(model.SteadyStartTime))
                            continue;
                        if (p.ParameterType == "LJL")
                        {
                            starttime = model.StartTime;
                            steadystarttime = model.SteadyStartTime;
                        }
                        //else if (p.ParameterType == "CLWD")
                        //{
                        //    starttime = model.StartTime;
                        //    steadystarttime = model.SteadyStartTime;
                        //}
                        else
                        {
                            starttime = Convert.ToDateTime(model.StartTime).AddSeconds(refstartadd).ToString("yyyy-MM-dd HH:mm:ss");
                            steadystarttime = Convert.ToDateTime(model.SteadyStartTime).AddSeconds(refsteadystartadd).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                }
                if (timelist.Count(j => j.ParameterCode == p.RefEndParameterCode) > 0)
                {
                    ParameterTimeModel model = timelist.Where(j => j.ParameterCode == p.RefEndParameterCode).ToList()[0];
                    if (string.IsNullOrEmpty(model.EndTime) || string.IsNullOrEmpty(model.SteadyEndTime))
                        continue;
                    if (p.ParameterType == "LJL")
                    {

                        endtime = Convert.ToDateTime(model.EndTime).AddSeconds(ljl).ToString("yyyy-MM-dd HH:mm:ss"); ;
                        steadyendtime = Convert.ToDateTime(model.SteadyEndTime).AddSeconds(ljl).ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    //else if (p.ParameterType == "CLWD")
                    //{
                    //    endtime = model.EndTime;
                    //    steadyendtime = model.SteadyEndTime;
                    //}
                    else
                    {
                        endtime = Convert.ToDateTime(model.EndTime).AddSeconds(refendadd).ToString("yyyy-MM-dd HH:mm:ss"); ;
                        steadyendtime = Convert.ToDateTime(model.SteadyEndTime).AddSeconds(refsteadyendadd).ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
                else
                {
                    //查询数据库
                    IList<ParameterTimeModel> list = dal.GetParameterTimelist(order.BatchNo, p.RefEndParameterCode);
                    if (list.Count == 0)
                    {
                        logbll.CreateLog(order, p.ParameterCode + "查找不到结束参考点，请检查！");
                    }
                    else
                    {
                        ParameterTimeModel model = list.Where(j => j.ParameterCode == p.RefEndParameterCode).ToList()[0];
                        if (string.IsNullOrEmpty(model.EndTime) || string.IsNullOrEmpty(model.SteadyEndTime))
                            continue;
                        if (p.ParameterType == "LJL")
                        {
                            endtime = Convert.ToDateTime(model.EndTime).AddSeconds(ljl).ToString("yyyy-MM-dd HH:mm:ss"); ;
                            steadyendtime = Convert.ToDateTime(model.SteadyEndTime).AddSeconds(ljl).ToString("yyyy-MM-dd HH:mm:ss"); ;
                        }
                        //else if (p.ParameterType == "CLWD")
                        //{
                        //    endtime = model.EndTime;
                        //    steadyendtime = model.SteadyEndTime;
                        //}
                        else
                        {
                            endtime = Convert.ToDateTime(model.EndTime).AddSeconds(refendadd).ToString("yyyy-MM-dd HH:mm:ss"); ;
                            steadyendtime = Convert.ToDateTime(model.SteadyEndTime).AddSeconds(refsteadyendadd).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                }
                ParameterTimeModel timemodel = new ParameterTimeModel();
                timemodel.ProductLineCode = order.ProductLineCode;
                timemodel.LineCode = order.LineCode;
                timemodel.ProductCode = order.ProductCode;
                timemodel.StageCode = order.StageCode;
                timemodel.ProcessCode = processcode;
                timemodel.ParameterCode = p.ParameterCode;
                timemodel.HisTag = p.TagCode;
                timemodel.StartTime = starttime;
                timemodel.EndTime = endtime;
                timemodel.SteadyStartTime = steadystarttime;
                timemodel.SteadyEndTime = steadyendtime;
                timelist.Add(timemodel);
            }
        }

        public void RefNewParameterCut(string processcode, OrderModel order, ref IList<ParameterTimeModel> timelist, int cyclic)
        {
            if (ProcessRefTaglist.Count(j => j.LineCode == order.LineCode && j.ProcessCode == processcode) == 0)
                return;
            IList<ProcessRefTag> templist = ProcessRefTaglist.Where(j => j.LineCode == order.LineCode && j.ProcessCode == processcode).ToList();
            int ljl = Convert.ToInt32(ConfigurationManager.AppSettings["ljladd"].ToString());
            foreach (ProcessRefTag p in templist)
            {
                string starttime = "";
                string endtime = "";
                if (timelist.Count(j => j.ParameterCode == p.RefStartParamerterCode) > 0)
                {
                    ParameterTimeModel model = timelist.Where(j => j.ParameterCode == p.RefStartParamerterCode).ToList()[0];
                    if ((p.CutType == "All"))
                    {
                        if (string.IsNullOrEmpty(model.StartTime) || string.IsNullOrEmpty(model.EndTime))
                            continue;
                        else
                        {
                            DataTable dt = dal.GetHisData(model.StartTime, model.EndTime, model.HisTag, cyclic);
                            starttime = FindRefStarttime(order, p.TagCode, dt, p.start_offset, p.startvalue);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(model.SteadyStartTime) || string.IsNullOrEmpty(model.SteadyEndTime))
                            continue;
                        else
                        {
                            DataTable dt = dal.GetHisData(model.SteadyStartTime, model.SteadyEndTime, model.HisTag, cyclic);
                            starttime = FindRefStarttime(order, p.TagCode, dt, p.start_offset, p.startvalue);
                        }
                    }
                }
                else
                {
                    //查询数据库
                    IList<ParameterTimeModel> list = dal.GetParameterTimelist(order.BatchNo, p.RefStartParamerterCode);
                    if (list.Count == 0)
                    {
                        logbll.CreateLog(order, p.ParameterCode + "查找不到开始参考点，请检查！");
                    }
                    else
                    {
                        ParameterTimeModel model = list.Where(j => j.ParameterCode == p.RefStartParamerterCode).ToList()[0];
                        if ((p.CutType == "All"))
                        {
                            if (string.IsNullOrEmpty(model.StartTime) || string.IsNullOrEmpty(model.EndTime))
                                continue;
                            else
                            {
                                DataTable dt = dal.GetHisData(model.StartTime, model.EndTime, model.HisTag, cyclic);
                                starttime = FindRefStarttime(order, p.TagCode, dt, p.start_offset, p.startvalue);
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(model.SteadyStartTime) || string.IsNullOrEmpty(model.SteadyEndTime))
                                continue;
                            else
                            {
                                DataTable dt = dal.GetHisData(model.SteadyStartTime, model.SteadyEndTime, model.HisTag, cyclic);
                                starttime = FindRefStarttime(order, p.TagCode, dt, p.start_offset, p.startvalue);
                            }
                        }
                    }
                }
                if (timelist.Count(j => j.ParameterCode == p.RefEndParameterCode) > 0)
                {
                    ParameterTimeModel model = timelist.Where(j => j.ParameterCode == p.RefEndParameterCode).ToList()[0];
                    if ((p.CutType == "All"))
                    {
                        if (string.IsNullOrEmpty(model.StartTime) || string.IsNullOrEmpty(model.EndTime))
                            continue;
                        else
                        {
                            DataTable dt = dal.GetHisData(model.StartTime, model.EndTime, model.HisTag, cyclic);
                            endtime = FindRefEndtime(order, p.TagCode, dt, p.end_offset, p.endvalue);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(model.SteadyStartTime) || string.IsNullOrEmpty(model.SteadyEndTime))
                            continue;
                        else
                        {
                            DataTable dt = dal.GetHisData(model.SteadyStartTime, model.SteadyEndTime, model.HisTag, cyclic);
                            endtime = FindRefEndtime(order, p.TagCode, dt, p.end_offset, p.endvalue);
                        }
                    }
                }
                else
                {
                    //查询数据库
                    IList<ParameterTimeModel> list = dal.GetParameterTimelist(order.BatchNo, p.RefEndParameterCode);
                    if (list.Count == 0)
                    {
                        logbll.CreateLog(order, p.ParameterCode + "查找不到结束参考点，请检查！");
                    }
                    else
                    {
                        ParameterTimeModel model = list.Where(j => j.ParameterCode == p.RefEndParameterCode).ToList()[0];
                        if ((p.CutType == "All"))
                        {
                            if (string.IsNullOrEmpty(model.StartTime) || string.IsNullOrEmpty(model.EndTime))
                                continue;
                            else
                            {
                                DataTable dt = dal.GetHisData(model.StartTime, model.EndTime, model.HisTag, cyclic);
                                endtime = FindRefEndtime(order, p.TagCode, dt, p.end_offset, p.endvalue);
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(model.SteadyStartTime) || string.IsNullOrEmpty(model.SteadyEndTime))
                                continue;
                            else
                            {
                                DataTable dt = dal.GetHisData(model.SteadyStartTime, model.SteadyEndTime, model.HisTag, cyclic);
                                endtime = FindRefEndtime(order, p.TagCode, dt, p.end_offset, p.endvalue);
                            }
                        }
                    }
                }
                if (timelist.Count(j => j.LineCode == order.LineCode && j.ProcessCode == processcode && j.HisTag == p.TagCode) == 0)
                {
                    if (p.CutType == "All")
                    {
                        ParameterTimeModel timemodel = new ParameterTimeModel();
                        timemodel.ProductLineCode = order.ProductLineCode;
                        timemodel.LineCode = order.LineCode;
                        timemodel.ProductCode = order.ProductCode;
                        timemodel.StageCode = order.StageCode;
                        timemodel.ProcessCode = processcode;
                        timemodel.ParameterCode = p.ParameterCode;
                        timemodel.HisTag = p.TagCode;
                        timemodel.StartTime = starttime;
                        timemodel.EndTime = endtime;
                        timemodel.SteadyStartTime = null;
                        timemodel.SteadyEndTime = null;
                        timelist.Add(timemodel);
                    }
                    else if (p.CutType == "Steady")
                    {
                        ParameterTimeModel timemodel = new ParameterTimeModel();
                        timemodel.ProductLineCode = order.ProductLineCode;
                        timemodel.LineCode = order.LineCode;
                        timemodel.ProductCode = order.ProductCode;
                        timemodel.StageCode = order.StageCode;
                        timemodel.ProcessCode = processcode;
                        timemodel.ParameterCode = p.ParameterCode;
                        timemodel.HisTag = p.TagCode;
                        timemodel.StartTime = null;
                        timemodel.EndTime = null;
                        timemodel.SteadyStartTime = starttime;
                        timemodel.SteadyEndTime = endtime;
                        timelist.Add(timemodel);
                    }
                }
                else
                {
                    if (p.CutType == "All")
                    {
                        timelist.Where(j => j.LineCode == order.LineCode && j.ProcessCode == processcode && j.HisTag == p.TagCode).ToList()[0].StartTime = string.IsNullOrEmpty(starttime) ? null : starttime;
                        timelist.Where(j => j.LineCode == order.LineCode && j.ProcessCode == processcode && j.HisTag == p.TagCode).ToList()[0].EndTime = string.IsNullOrEmpty(endtime) ? null : endtime;
                    }
                    else if (p.CutType == "Steady")
                    {
                        timelist.Where(j => j.LineCode == order.LineCode && j.ProcessCode == processcode && j.HisTag == p.TagCode).ToList()[0].SteadyStartTime = string.IsNullOrEmpty(starttime) ? null : starttime;
                        timelist.Where(j => j.LineCode == order.LineCode && j.ProcessCode == processcode && j.HisTag == p.TagCode).ToList()[0].SteadyEndTime = string.IsNullOrEmpty(endtime) ? null : endtime;
                    }
                }
            }
        }

        public string ContinueLJL(string parametercode, string tag, string value, OrderModel order, IList<ContinueBatchModel> continuelist, int ljlcyclic)
        {
            if (continuelist.Count(j => j.BatchNo == order.BatchNo && j.StageCode == order.StageCode) == 0)
                return value;
            else
            {
                ContinueBatchModel model = continuelist.Where(j => j.BatchNo == order.BatchNo && j.StageCode == order.StageCode).ToList()[0];
                if (continuelist.Count(j => j.IUID == model.IUID && j.StageCode == order.StageCode && j.SortNo < model.SortNo) == 0)
                    return value;
                else
                {
                    ContinueBatchModel upmodel = continuelist.Where(j => j.IUID == model.IUID && j.StageCode == order.StageCode && j.SortNo < model.SortNo).OrderByDescending(j => j.SortNo).ToList()[0];
                    string upbatchno = upmodel.BatchNo;
                    if (string.IsNullOrEmpty(upbatchno))
                    {
                        logbll.CreateLog(order, parametercode + "计算累计量时,已存在连批记录，但是批次号为空!");
                        return value;
                    }
                    else
                    {
                        //根据批次号获取上一批该参数点的开始时间和结束时间
                        DataTable dt2 = dal.GetParameterTimeList(upbatchno, "'" + parametercode + "'");
                        if (dt2.Rows.Count == 0)
                        {
                            logbll.CreateLog(order, parametercode + "计算累计量时,已存在连批记录，前一批:" + upbatchno + "的该参数点在SPC归集表中不存在!");
                            return value;
                        }
                        else
                        {
                            string starttime = dt2.Rows[0]["StartTime"].ToString();
                            string endtime = dt2.Rows[0]["EndTime"].ToString();
                            if (string.IsNullOrEmpty(starttime) || string.IsNullOrEmpty(endtime))
                            {
                                logbll.CreateLog(order, parametercode + "计算累计量时,已存在连批记录，前一批:" + upbatchno + "的该参数点在SPC归集表中开始时间和结束时间为空!");
                                return value;
                            }
                            //获取实时数据
                            DataTable dt3 = dal.GetHisData(starttime, endtime, tag, ljlcyclic);
                            string maxvalue = GetMax(dt3);
                            string result = (Convert.ToDouble(value) - Convert.ToDouble(maxvalue)).ToString();
                            return result;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 正常判断时间
        /// </summary>
        /// <param name="processcode">工序</param>
        /// <param name="order">工单信息</param>
        /// <param name="maintaglist">主要参数点列表</param>
        /// <param name="shutflag">断料标识，无用</param>
        /// <param name="timelist">参数时间列表</param>
        public void DontContinueCut(string processcode, OrderModel order, IList<ProcessMainTag> maintaglist, bool shutflag, ref IList<ParameterTimeModel> timelist, ref string pr)
        {
            foreach (ProcessMainTag maintag in maintaglist)
            {
                //获取截取规则
                if (Cutlist.Count(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode && j.ParameterCode == maintag.ParameterCode && j.Flag == "all") == 0)
                    continue;
                ProcessCutRuleModel cutmodel = Cutlist.Where(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode && j.ParameterCode == maintag.ParameterCode && j.Flag == "all").ToList()[0];
                //获取样本间时间间隔，读取配置文件
                //根据实时数据点获取数据
                DataTable dt = dal.GetHisData(order.StartTime, order.EndTime, maintag.HisTag, cyclic);
                //获取完整数据的时间点
                int sadd = Convert.ToInt32(ConfigurationManager.AppSettings["sadd"].ToString());//开始偏移量,读取配置文件
                int eadd = Convert.ToInt32(ConfigurationManager.AppSettings["eadd"].ToString());//结束偏移量,读取配置文件
                string starttime = FindAllStartTime(order, maintag.HisTag, dt, cutmodel.Value, sadd, shutflag, cyclic, false, 0);
                string endtime = FindAllEndTime(order, maintag.HisTag, dt, cutmodel.Value, eadd, shutflag, cyclic, 0);
                //根据开始时间、结束时间重新获取数据
                if (string.IsNullOrEmpty(starttime))
                    starttime = order.StartTime;
                if (string.IsNullOrEmpty(endtime))
                    endtime = order.EndTime;
                DataTable sdt = dal.GetHisData(starttime, endtime, maintag.HisTag, cyclic);
                if (Cutlist.Count(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode && j.ParameterCode == maintag.ParameterCode && j.Flag == "steady") == 0)
                    continue;
                ProcessCutRuleModel steadycut = Cutlist.Where(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode && j.ParameterCode == maintag.ParameterCode && j.Flag == "steady").ToList()[0];
                string steadystarttime = FindSteadyStartTime(order, maintag.ParameterCode, sdt, steadycut.Value, steadycut.AddTime, cyclic);
                string steadyendtime = FindSteadyEndTime(order, maintag.ParameterCode, sdt, steadycut.Value, steadycut.AddTime, cyclic);
                if (maintag.ParameterType == "LL")
                {
                    if (!string.IsNullOrEmpty(steadystarttime) && !string.IsNullOrEmpty(steadyendtime))
                    {
                        DataTable tempdt = dal.GetHisData(steadystarttime, steadyendtime, maintag.HisTag, cyclic);
                        DataRow[] drs = tempdt.Select("vValue<='0'");
                        if (tempdt.Rows.Count > 0)
                        {
                            if (Convert.ToDouble(drs.Length) / Convert.ToDouble(tempdt.Rows.Count) > 0.9)
                            {
                                //流量占比超过90%时，视为不走该电子秤
                                pr = processcode;
                            }
                            else
                            {
                                int continuetime = Convert.ToInt32(ConfigurationManager.AppSettings["continuetimemin"].ToString());//断料持续时间，读取配置文件
                                CheckShutDown(tempdt, processcode, maintag.ParameterCode, cutmodel.Value, continuetime, order);
                            }
                        }
                    }
                }
                ParameterTimeModel timemodel = new ParameterTimeModel();
                timemodel.ProductLineCode = order.ProductLineCode;
                timemodel.LineCode = order.LineCode;
                timemodel.ProductCode = order.ProductCode;
                timemodel.StageCode = order.StageCode;
                timemodel.ProcessCode = processcode;
                timemodel.ParameterCode = maintag.ParameterCode;
                timemodel.HisTag = maintag.HisTag;
                timemodel.StartTime = starttime;
                timemodel.EndTime = endtime;
                timemodel.SteadyStartTime = steadystarttime;
                timemodel.SteadyEndTime = steadyendtime;
                timelist.Add(timemodel);
            }
        }

        /// <summary>
        /// 针对切烘段连批的判断开始时间和结束时间
        /// </summary>
        /// <param name="Processcode">工序</param>
        /// <param name="order">工单信息</param>
        /// <param name="maintaglist">主要参数点列表</param>
        /// <param name="shutflag">断料标识，无用</param>
        /// <param name="continuelist">连批记录</param>
        /// <param name="timelist">参数时间列表</param>
        public void ContinueCut(string Processcode, OrderModel order, IList<ProcessMainTag> maintaglist, bool shutflag, IList<ContinueBatchModel> continuelist, ref IList<ParameterTimeModel> timelist)
        {

            if (continuelist.Count(j => j.BatchNo == order.BatchNo && order.LineCode == j.LineCode && j.StageCode == order.StageCode) > 0)
            {
                string iuid = continuelist.Where(j => j.BatchNo == order.BatchNo && order.LineCode == j.LineCode && j.StageCode == order.StageCode).ToList()[0].IUID;
                IList<ContinueBatchModel> tempcontinuelist = dal.GetContinuebatchlist(iuid);
                int sortno = tempcontinuelist.Where(j => j.BatchNo == order.BatchNo && order.LineCode == j.LineCode && j.StageCode == order.StageCode).ToList()[0].SortNo;
                int temptcount = tempcontinuelist.Where(j => j.BatchNo == order.BatchNo && order.LineCode == j.LineCode && j.StageCode == order.StageCode).ToList().Count;
                foreach (ProcessMainTag maintag in maintaglist)
                {
                    if (Cutlist.Count(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode && j.ParameterCode == maintag.ParameterCode && j.Flag == "steady") == 0)
                        continue;
                    if (Cutlist.Count(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode && j.ParameterCode == maintag.ParameterCode && j.Flag == "all") == 0)
                        continue;
                    //获取截取规则
                    ProcessCutRuleModel steadycut = Cutlist.Where(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode && j.ParameterCode == maintag.ParameterCode && j.Flag == "steady").ToList()[0];
                    ProcessCutRuleModel cutmodel = Cutlist.Where(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode && j.ParameterCode == maintag.ParameterCode && j.Flag == "all").ToList()[0];
                    //获取样本间时间间隔，读取配置文件
                    //根据实时数据点获取数据
                    DataTable dt = dal.GetHisData(order.StartTime, order.EndTime, maintag.HisTag, cyclic);
                    //获取完整数据的时间点
                    int sadd = Convert.ToInt32(ConfigurationManager.AppSettings["sadd"].ToString());//开始偏移量,读取配置文件
                    int eadd = Convert.ToInt32(ConfigurationManager.AppSettings["eadd"].ToString());//结束偏移量,读取配置文件
                    if (sortno == 1)
                    {
                        string starttime = FindAllStartTime(order, maintag.HisTag, dt, cutmodel.Value, sadd, shutflag, cyclic, false, 0);
                        string endtime = order.EndTime;
                        if (string.IsNullOrEmpty(starttime))
                            starttime = order.StartTime;
                        DataTable sdt = dal.GetHisData(starttime, endtime, maintag.HisTag, cyclic);
                        string steadystarttime = FindSteadyStartTime(order, maintag.ParameterCode, sdt, steadycut.Value, steadycut.AddTime, cyclic);
                        string steadyendtime = order.EndTime;
                        if (maintag.ParameterType == "LL")
                        {
                            if (!string.IsNullOrEmpty(steadystarttime) && !string.IsNullOrEmpty(steadyendtime))
                            {
                                DataTable tempdt = dal.GetHisData(steadystarttime, steadyendtime, maintag.HisTag, cyclic);
                                int continuetime = Convert.ToInt32(ConfigurationManager.AppSettings["continuetimemin"].ToString());//断料持续时间，读取配置文件
                                CheckShutDown(tempdt, Processcode, maintag.ParameterCode, cutmodel.Value, continuetime, order);
                            }
                        }
                        ParameterTimeModel timemodel = new ParameterTimeModel();
                        timemodel.ProductLineCode = order.ProductLineCode;
                        timemodel.LineCode = order.LineCode;
                        timemodel.ProductCode = order.ProductCode;
                        timemodel.StageCode = order.StageCode;
                        timemodel.ProcessCode = Processcode;
                        timemodel.ParameterCode = maintag.ParameterCode;
                        timemodel.HisTag = maintag.HisTag;
                        timemodel.StartTime = starttime;
                        timemodel.EndTime = endtime;
                        timemodel.SteadyStartTime = steadystarttime;
                        timemodel.SteadyEndTime = steadyendtime;
                        timelist.Add(timemodel);
                    }
                    else if (sortno == temptcount)
                    {
                        string starttime = order.StartTime;
                        string endtime = FindAllEndTime(order, maintag.HisTag, dt, cutmodel.Value, eadd, shutflag, cyclic, 0);
                        if (string.IsNullOrEmpty(endtime))
                            endtime = order.EndTime;
                        DataTable sdt = dal.GetHisData(starttime, endtime, maintag.HisTag, cyclic);
                        string steadystarttime = order.StartTime;
                        string steadyendtime = FindSteadyEndTime(order, maintag.ParameterCode, sdt, steadycut.Value, steadycut.AddTime, cyclic);
                        if (maintag.ParameterType == "LL")
                        {
                            if (!string.IsNullOrEmpty(steadystarttime) && !string.IsNullOrEmpty(steadyendtime))
                            {
                                DataTable tempdt = dal.GetHisData(steadystarttime, steadyendtime, maintag.HisTag, cyclic);
                                int continuetime = Convert.ToInt32(ConfigurationManager.AppSettings["continuetimemin"].ToString());//断料持续时间，读取配置文件
                                CheckShutDown(tempdt, Processcode, maintag.ParameterCode, cutmodel.Value, continuetime, order);
                            }
                        }
                        ParameterTimeModel timemodel = new ParameterTimeModel();
                        timemodel.ProductLineCode = order.ProductLineCode;
                        timemodel.LineCode = order.LineCode;
                        timemodel.ProductCode = order.ProductCode;
                        timemodel.StageCode = order.StageCode;
                        timemodel.ProcessCode = Processcode;
                        timemodel.ParameterCode = maintag.ParameterCode;
                        timemodel.HisTag = maintag.HisTag;
                        timemodel.StartTime = starttime;
                        timemodel.EndTime = endtime;
                        timemodel.SteadyStartTime = steadystarttime;
                        timemodel.SteadyEndTime = steadyendtime;
                        timelist.Add(timemodel);
                    }
                    else
                    {
                        string starttime = order.StartTime;
                        string endtime = order.EndTime;
                        string steadystarttime = order.StartTime;
                        string steadyendtime = order.EndTime;
                        if (maintag.ParameterType == "LL")
                        {
                            DataTable tempdt = dal.GetHisData(steadystarttime, steadyendtime, maintag.HisTag, cyclic);
                            int continuetime = Convert.ToInt32(ConfigurationManager.AppSettings["continuetimemin"].ToString());//断料持续时间，读取配置文件
                            CheckShutDown(tempdt, Processcode, maintag.ParameterCode, cutmodel.Value, continuetime, order);
                        }
                        ParameterTimeModel timemodel = new ParameterTimeModel();
                        timemodel.ProductLineCode = order.ProductLineCode;
                        timemodel.LineCode = order.LineCode;
                        timemodel.ProductCode = order.ProductCode;
                        timemodel.StageCode = order.StageCode;
                        timemodel.ProcessCode = Processcode;
                        timemodel.ParameterCode = maintag.ParameterCode;
                        timemodel.HisTag = maintag.HisTag;
                        timemodel.StartTime = starttime;
                        timemodel.EndTime = endtime;
                        timemodel.SteadyStartTime = steadystarttime;
                        timemodel.SteadyEndTime = steadyendtime;
                        timelist.Add(timemodel);
                    }
                }
            }
        }

        /// <summary>
        /// 去掉工单暂停时间段
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <param name="dt">原始数据</param>
        /// <param name="list">暂停记录</param>
        /// <returns></returns>
        public DataTable AssembleData(OrderModel order, DataTable dt, IList<PauseOrderModel> list)
        {
            DataTable rdt = dt.Clone();
            foreach (PauseOrderModel m in list)
            {
                if (string.IsNullOrEmpty(m.StartTime) || string.IsNullOrEmpty(m.EndTime))
                    logbll.CreateLog(order, "该工单存在暂停记录，但是开始时间和结束时间为空！");
                else
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string time = dt.Rows[i]["DateTime"].ToString();
                        if (!(Convert.ToDateTime(time) >= Convert.ToDateTime(m.StartTime) && Convert.ToDateTime(time) <= Convert.ToDateTime(m.EndTime)))
                        {
                            DataRow dr = rdt.NewRow();
                            dr["Tag"] = dt.Rows[i]["Tag"].ToString();
                            dr["DateTime"] = dt.Rows[i]["DateTime"].ToString();
                            dr["vValue"] = dt.Rows[i]["vValue"].ToString();
                            rdt.Rows.Add(dr);
                        }
                    }
                }
            }
            if (rdt.Rows.Count > 0)
                return rdt;
            else
                return dt;
        }

        /// <summary>
        /// 获取稳态开始时间
        /// </summary>
        /// <param name="dt">原始数据</param>
        /// <param name="value">稳态临界值</param>
        /// <param name="add">延迟时长</param>
        /// <param name="cyclic">样本周期</param>
        /// <returns></returns>
        public string FindSteadyStartTime(OrderModel order, string parametercode, DataTable dt, string value, int add, int cyclic)
        {
            try
            {
                if (dt.Rows.Count == 0)
                    return "";
                int index = 0;
                int count = add * 1000 / cyclic;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string tempvalue = dt.Rows[i]["vValue"].ToString();
                    if (Convert.ToDouble(tempvalue) >= Convert.ToDouble(value))
                    {
                        index = i;
                        break;
                    }
                }
                index = index + count;
                return dt.Rows[index]["DateTime"].ToString();
            }
            catch (Exception e)
            {
                logbll.CreateLog(order, parametercode + "找稳态开始时间报错:" + e.Message);
                return "";
            }
        }

        /// <summary>
        /// 获取稳态结束时间
        /// </summary>
        /// <param name="dt">原始数据</param>
        /// <param name="value">稳态临界值</param>
        /// <param name="add">延迟时长</param>
        /// <param name="cyclic">样本周期</param>
        /// <returns></returns>
        public string FindSteadyEndTime(OrderModel order, string parametercode, DataTable dt, string value, int add, int cyclic)
        {
            try
            {
                if (dt.Rows.Count == 0)
                    return "";
                int index = dt.Rows.Count - 1;
                int count = add * 1000 / cyclic;
                for (int i = dt.Rows.Count - 1; i > 0; i--)
                {
                    string tempvalue = dt.Rows[i]["vValue"].ToString();
                    if (Convert.ToDouble(tempvalue) >= Convert.ToDouble(value))
                    {
                        index = i;
                        break;
                    }
                }
                index = index - count;
                return dt.Rows[index]["DateTime"].ToString();
            }
            catch (Exception e)
            {
                logbll.CreateLog(order, parametercode + "找稳态结束时间报错:" + e.Message);
                return "";
            }
        }

        /// <summary>
        /// 递归获取完整数据的开始时间点
        /// </summary>
        /// <param name="dt">原始数据</param>
        /// <param name="value">完整数据临界值</param>
        /// <param name="add">下次读取完整数据偏移量</param>
        /// <param name="shutflag">断料标识</param>
        /// <param name="cyclic">样本周期</param>
        /// <param name="flag">递归标识</param>
        /// <returns>开始时间</returns>
        public string FindAllStartTime(OrderModel order, string histag, DataTable dt, string value, int add, bool shutflag, int cyclic, bool flag, int count)
        {
            int maxcount = Convert.ToInt32(ConfigurationManager.AppSettings["count"].ToString());
            count++;
            if (dt.Rows.Count == 0)
                return "";
            string orderstarttime = dt.Rows[0]["DateTime"].ToString();
            string orderendtime = dt.Rows[dt.Rows.Count - 1]["DateTime"].ToString();
            string onevalue = dt.Rows[0]["vValue"].ToString();
            if (count > maxcount)
            {
                logbll.CreateLog(order, histag + "递归超过最大次数");
                return orderstarttime;
            }
            if (Convert.ToDouble(onevalue) > Convert.ToDouble(value) && !flag)
            {
                orderstarttime = Convert.ToDateTime(orderstarttime).AddSeconds(add).ToString("yyyy-MM-dd HH:mm:ss");
                DataTable rdt = dal.GetHisData(orderstarttime, orderendtime, histag, cyclic);
                return FindAllStartTime(order, histag, rdt, value, add, shutflag, cyclic, false, count);
            }
            else
            {
                flag = true;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string tempvalue = dt.Rows[i]["vValue"].ToString();
                    if (Convert.ToDouble(tempvalue) > Convert.ToDouble(value))
                    {
                        string tempttime = CheckZero(dt, cyclic, add, value);
                        if (string.IsNullOrEmpty(tempttime))
                        {
                            string rtime = dt.Rows[i]["DateTime"].ToString();
                            return rtime;
                        }
                        else
                        {
                            orderstarttime = tempttime;
                            DataTable rdt = dal.GetHisData(orderstarttime, orderendtime, histag, cyclic);
                            return FindAllStartTime(order, histag, rdt, value, add, shutflag, cyclic, true, count);
                        }
                    }
                    if (i == dt.Rows.Count - 1)
                        return "";
                }
            }
            return "";
        }

        public string FindRefStarttime(OrderModel order, string histag, DataTable dt, string addtime, string value)
        {
            if (string.IsNullOrEmpty(addtime) && string.IsNullOrEmpty(value))
            {
                //说明是同步
                if (dt.Rows.Count > 0)
                    return dt.Rows[0]["DateTime"].ToString();
                else
                    return "";
            }

            else if (string.IsNullOrEmpty(addtime) && !string.IsNullOrEmpty(value))
            {
                //说明要重新取临界值，但是时间不再偏移
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    double tempvalue = Convert.ToDouble(dt.Rows[i]["Value"].ToString());
                    if (tempvalue >= Convert.ToDouble(value))
                        return dt.Rows[i]["DateTime"].ToString();
                }
                return "";
            }
            else if (!string.IsNullOrEmpty(addtime) && string.IsNullOrEmpty(value))
            {
                //说明只存在时间偏移
                if (dt.Rows.Count > 0)
                    return Convert.ToDateTime(dt.Rows[0]["DateTime"].ToString()).AddSeconds(Convert.ToDouble(addtime)).ToString("yyyy-MM-dd HH:mm:ss");
                return "";
            }
            else if (!string.IsNullOrEmpty(addtime) && !string.IsNullOrEmpty(value))
            {
                //说明存在临界值和偏移时间
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    double tempvalue = Convert.ToDouble(dt.Rows[i]["Value"].ToString());
                    if (tempvalue >= Convert.ToDouble(value))
                        return Convert.ToDateTime(dt.Rows[i]["DateTime"].ToString()).AddSeconds(Convert.ToDouble(addtime)).ToString("yyyy-MM-dd HH:mm:ss");
                }
                return "";
            }
            else
                return "";
        }

        public string FindRefEndtime(OrderModel order, string histag, DataTable dt, string addtime, string value)
        {
            if (string.IsNullOrEmpty(addtime) && string.IsNullOrEmpty(value))
            {
                //说明是同步
                if (dt.Rows.Count > 0)
                    return dt.Rows[dt.Rows.Count - 1]["DateTime"].ToString();
                else
                    return "";
            }
            else if (string.IsNullOrEmpty(addtime) && !string.IsNullOrEmpty(value))
            {
                //说明要重新取临界值，但是时间不再偏移
                for (int i = dt.Rows.Count - 1; i > 0; i--)
                {
                    double tempvalue = Convert.ToDouble(dt.Rows[i]["Value"].ToString());
                    if (tempvalue >= Convert.ToDouble(value))
                        return dt.Rows[i]["DateTime"].ToString();
                }
                return "";
            }
            else if (!string.IsNullOrEmpty(addtime) && string.IsNullOrEmpty(value))
            {
                //说明只存在时间偏移
                if (dt.Rows.Count > 0)
                    return Convert.ToDateTime(dt.Rows[dt.Rows.Count - 1]["DateTime"].ToString()).AddSeconds(Convert.ToDouble(addtime)).ToString("yyyy-MM-dd HH:mm:ss");
                return "";
            }
            else if (!string.IsNullOrEmpty(addtime) && !string.IsNullOrEmpty(value))
            {
                //说明存在临界值和偏移时间
                for (int i = dt.Rows.Count - 1; i > 0; i--)
                {
                    double tempvalue = Convert.ToDouble(dt.Rows[i]["Value"].ToString());
                    if (tempvalue >= Convert.ToDouble(value))
                        return Convert.ToDateTime(dt.Rows[i]["DateTime"].ToString()).AddSeconds(Convert.ToDouble(addtime)).ToString("yyyy-MM-dd HH:mm:ss");
                }
                return "";
            }
            else
                return "";
        }

        /// <summary>
        /// 判断料头不稳定，导致跳点
        /// </summary>
        /// <param name="dt">原始数据</param>
        /// <param name="cyclic">取样周期</param>
        /// <param name="add">延迟时间，单位秒</param>
        /// <param name="value">临界值</param>
        /// <returns></returns>
        public string CheckZero(DataTable dt, int cyclic, int add, string value)
        {
            string rtime = "";
            int count = add * 1000 / cyclic;
            string starttime = dt.Rows[0]["DateTime"].ToString();
            int rcount = 0;
            if (count > dt.Rows.Count)
                rcount = dt.Rows.Count;
            else
                rcount = count;
            DataTable tempdt = dt.Clone();
            for (int i = 0; i < rcount; i++)
            {
                if (Convert.ToDouble(dt.Rows[i]["vValue"].ToString()) <= Convert.ToDouble(value))
                {
                    DataRow dr = tempdt.NewRow();
                    dr["DateTime"] = dt.Rows[i]["DateTime"].ToString();
                    dr["vValue"] = dt.Rows[i]["vValue"].ToString();
                    tempdt.Rows.Add(dr);
                }
            }
            for (int i = 0; i < tempdt.Rows.Count - 1; i++)
            {
                string time = tempdt.Rows[i]["DateTime"].ToString();
                string temptime = tempdt.Rows[i + 1]["DateTime"].ToString();
                if (Convert.ToDateTime(time).ToString("yyyy-MM-dd HH:mm:ss").Equals(Convert.ToDateTime(temptime).AddSeconds(-cyclic / 1000).ToString("yyyy-MM-dd HH:mm:ss")))
                {
                    rtime = temptime;
                }
            }
            return rtime;
        }

        /// <summary>
        /// 递归获取完整数据的结束时间点
        /// </summary>
        /// <param name="dt">原始数据</param>
        /// <param name="value">完整数据临界值</param>
        /// <param name="add">下次读取完整数据偏移量</param>
        /// <param name="shutflag">断料标识</param>
        /// <param name="cyclic">样本周期</param>
        /// <returns>结束时间</returns>
        public string FindAllEndTime(OrderModel order, string histag, DataTable dt, string value, int add, bool shutflag, int cyclic, int count)
        {
            int maxcount = Convert.ToInt32(ConfigurationManager.AppSettings["count"].ToString());
            count++;
            if (dt.Rows.Count == 0)
                return "";
            string orderstarttime = dt.Rows[0]["DateTime"].ToString();
            string orderendtime = dt.Rows[dt.Rows.Count - 1]["DateTime"].ToString();
            string lastvalue = dt.Rows[dt.Rows.Count - 1]["vValue"].ToString();
            if (Convert.ToDouble(lastvalue) > Convert.ToDouble(value))
            {
                orderendtime = Convert.ToDateTime(orderendtime).AddSeconds(add).ToString("yyyy-MM-dd HH:mm:ss");
                DataTable rdt = dal.GetHisData(orderstarttime, orderendtime, histag, cyclic);
                return FindAllEndTime(order, histag, rdt, value, add, shutflag, cyclic, count);
            }
            else
            {
                for (int i = dt.Rows.Count - 1; i > 0; i--)
                {
                    string tempvalue = dt.Rows[i]["vValue"].ToString();
                    if (Convert.ToDouble(tempvalue) > Convert.ToDouble(value))
                    {
                        string rtime = dt.Rows[i]["DateTime"].ToString();
                        return rtime;
                    }
                    if (i == 0)
                        return "";
                }
            }
            return "";
        }

        /// <summary>
        /// 获取完整数据的开始时间点
        /// </summary>
        /// <param name="dt">原始数据表</param>
        /// <param name="value">临界值</param>
        /// <param name="continuetime">持续多长时间，主要是对完整开始点和结束点的时长比较</param>
        /// <returns></returns>
        public string CheckStartTime(DataTable dt, string value, int continuetime)
        {
            DataRow[] drs = dt.Select("vValue<='" + value + "'");
            if (drs.Length == 0)
                return "-1";
            //判断相邻两个数据点之间的时间差
            for (int i = 0; i < drs.Length - 1; i++)
            {
                DateTime fronttime = Convert.ToDateTime(drs[i]["DateTime"].ToString());
                DateTime lastertime = Convert.ToDateTime(drs[i + 1]["DateTime"].ToString());
                TimeSpan t = lastertime - fronttime;
                int count = Convert.ToInt32(t.TotalSeconds.ToString());
                if (count >= continuetime)
                {
                    return fronttime.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            return "";
        }

        /// <summary>
        /// 判断是否存在停机断料，主要是针对流量
        /// </summary>
        /// <param name="dt">未处理的原始数据</param>
        /// <param name="value">临界值</param>
        /// <param name="continuetime">持续时间</param>
        /// <param name="order">工单信息</param>
        /// <returns></returns>
        public void CheckShutDown(DataTable dt, string processcode, string parametercode, string value, int continuetime, OrderModel order)
        {
            DataRow[] drs = dt.Select("vValue<='" + value + "'");
            string starttime = "";
            string endtime = "";
            bool flag = true;
            //判断相邻两个数据点之间的时间差
            for (int i = 0; i < drs.Length - 1; i++)
            {
                DateTime fronttime = Convert.ToDateTime(drs[i]["DateTime"].ToString());
                DateTime lastertime = Convert.ToDateTime(drs[i + 1]["DateTime"].ToString());
                TimeSpan t = lastertime - fronttime;
                int count = t.Seconds;

                if (count != cyclic / 1000)
                {
                    flag = false;

                }
                if (flag)
                {
                    //说明为连续值
                    if (string.IsNullOrEmpty(starttime))
                        starttime = fronttime.ToString("yyyy-MM-dd HH:mm:ss");
                    if ((i + 1) == drs.Length - 1)
                    {
                        endtime = lastertime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(endtime))
                        endtime = fronttime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                if (!string.IsNullOrEmpty(starttime) && !string.IsNullOrEmpty(endtime))
                {
                    TimeSpan t1 = Convert.ToDateTime(endtime) - Convert.ToDateTime(starttime);
                    int count1 = t1.Seconds;
                    int maxcount = Convert.ToInt32(ConfigurationManager.AppSettings["continuetimemax"].ToString());
                    if (count1 >= continuetime && count1 <= maxcount)
                    {
                        OrderShutDownModel model = new OrderShutDownModel();
                        model.BatchNo = order.BatchNo;
                        model.OrderNo = order.OrderNo;
                        model.ProductLineCode = order.ProductLineCode;
                        model.LineCode = order.LineCode;
                        model.StageCode = order.StageCode;
                        model.ShutStartTime = starttime;
                        model.ShutEndTime = endtime;
                        model.ProcessCode = processcode;
                        model.ParameterCode = parametercode;
                        shutlist.Add(model);
                    }
                    //重置标示
                    starttime = "";
                    endtime = "";
                    flag = true;
                }
            }
        }

        public void CheckShutDown(string parametercode, DataTable dt, string value, int continuetime, OrderModel order)
        {
            DataRow[] drs = dt.Select("vValue<='" + value + "'");
            string starttime = "";
            string endtime = "";
            bool flag = true;
            //判断相邻两个数据点之间的时间差
            for (int i = 0; i < drs.Length - 1; i++)
            {
                DateTime fronttime = Convert.ToDateTime(drs[i]["DateTime"].ToString());
                DateTime lastertime = Convert.ToDateTime(drs[i + 1]["DateTime"].ToString());
                TimeSpan t = lastertime - fronttime;
                int count = t.Seconds;

                if (count != cyclic / 1000)
                {
                    flag = false;

                }
                if (flag)
                {
                    //说明为连续值
                    if (string.IsNullOrEmpty(starttime))
                        starttime = fronttime.ToString("yyyy-MM-dd HH:mm:ss");
                    if ((i + 1) == drs.Length - 1)
                    {
                        endtime = lastertime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(endtime))
                        endtime = fronttime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                if (!string.IsNullOrEmpty(starttime) && !string.IsNullOrEmpty(endtime))
                {
                    TimeSpan t1 = Convert.ToDateTime(endtime) - Convert.ToDateTime(starttime);
                    int count1 = t1.Seconds;
                    int maxcount = Convert.ToInt32(ConfigurationManager.AppSettings["continuetimemax"].ToString());
                    if (count1 >= continuetime && count1 <= maxcount)
                    {
                        OrderShutDownModel model = new OrderShutDownModel();
                        model.BatchNo = order.BatchNo;
                        model.OrderNo = order.OrderNo;
                        model.ProductLineCode = order.ProductLineCode;
                        model.LineCode = order.LineCode;
                        model.StageCode = order.StageCode;
                        model.ShutStartTime = starttime;
                        model.ShutEndTime = endtime;
                        model.ParameterCode = parametercode;
                        shutlist.Add(model);
                    }
                    //重置标示
                    starttime = "";
                    endtime = "";
                    flag = true;
                }
            }
        }

        public bool CheckTime(OrderModel order, string time)
        {
            DateTime dtnow = DateTime.Now;
            DateTime orderendtime = Convert.ToDateTime(time);
            TimeSpan t = dtnow.Subtract(orderendtime);
            double s = t.TotalSeconds;
            double diff = Convert.ToDouble(ConfigurationManager.AppSettings["diff"].ToString());
            if (s < diff && s > 0)
            {
                logbll.CreateLog(order, "工单结束时间与当前时间差过小，请等待！");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 基础数据检查
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <returns>true：满足截取条件，false:不满足条件</returns>
        public bool CheckData(OrderModel order)
        {
            bool check = true;
            if (string.IsNullOrEmpty(order.StartTime) || string.IsNullOrEmpty(order.EndTime))
            {
                logbll.CreateLog(order, "工单开始时间、结束时间为空，请检查！");
                if (check)
                    check = false;
            }
            if (ProcessMaintaglist.Count(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode) == 0)
            {
                logbll.CreateLog(order, "工单对应的工艺段在主要参数点表中无法对应，请检查！");
                if (check)
                    check = false;
            }
            //主要参数点
            IList<ProcessMainTag> maintaglist = ProcessMaintaglist.Where(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode).ToList();
            foreach (ProcessMainTag maintag in maintaglist)
            {
                if (string.IsNullOrEmpty(maintag.ParameterCode) || string.IsNullOrEmpty(maintag.HisTag))
                {
                    logbll.CreateLog(order, maintag.ProcessCode + ":" + maintag.ParameterCode + ":工单对应的工序主要参数点地址为空，请检查！");
                    if (check)
                        check = false;
                }
            }
            foreach (ProcessMainTag maintag in maintaglist)
            {
                if (Cutlist.Count(j => j.LineCode == order.LineCode && j.StageCode == order.StageCode && j.ParameterCode == maintag.ParameterCode) == 0)
                {
                    logbll.CreateLog(order, maintag.ProcessCode + ":" + maintag.ParameterCode + ":工单对应的工序主要参数点截取规则不存在，请检查！");
                    if (check)
                        check = false;
                }
            }
            return check;
        }

        /// <summary>
        /// 根据工单和检验类型获取工艺标准
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <param name="checktype">检验类型</param>
        /// <returns></returns>
        public IList<StandardDetailModel> GetStandardList(OrderModel order, string checktype)
        {
            IList<StandardMainModel> mainlist = dal.GetStandardMainlist(order.BrandCode);//dal.GetCheckStandardMain(order.BrandCode, checktype);
            IList<StandardDetailModel> detailist = new List<StandardDetailModel>();
            if (mainlist.Count == 0)
            {
                // logbll.CreateLog(order, "无法获取检验标准，以生产工艺技术标准计算！");
                logbll.CreateLog(order, "无法获取生产工艺技术标准，请检查！");
                //mainlist = dal.GetStandardMainlist(order.BrandCode);
                //if (mainlist.Count == 0)
                //{
                //    logbll.CreateLog(order, "无法获取生产工艺技术标准，请检查！");
                //}
            }
            else
            {
                detailist = dal.GetStandardDetaillist(mainlist[0].VersionID);
            }
            return detailist;
        }

        /// <summary>
        /// 计算物耗信息
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <param name="list">工艺段投入产出配置信息</param>
        /// <param name="reflist">参考点信息</param>
        public void MaterielConsume(OrderModel order, IList<CPConfigModel> list, IList<ProcessRefTag> reflist, IList<InOutConfigModel> inoutlist, IList<ParameterModel> parameterlist, IList<ContinueBatchModel> continuelist, ref Hashtable ha)
        {
            string checktype = ConfigurationManager.AppSettings["checktype"].ToString();//读取配置文件
            int ljlcyclic = Convert.ToInt32(ConfigurationManager.AppSettings["ljlcyclic"].ToString());
            Hashtable ht = new Hashtable();
            //1.更新工单开始时间
            //2.计算掺配量
            //3.计算投入产出量
            if (list.Count(j => j.StageCode == order.StageCode) == 0)
            {
                logbll.CreateLog(order, "该工单不存在物料掺配信息，请检查配置表！");
            }
            else
            {
                //计算掺配量和掺配比例
                ha = GetCPData(order, list, reflist, parameterlist, ljlcyclic, continuelist);
                IList<StandardDetailModel> standdetaillist = GetStandardList(order, checktype);
                ht = ComputerCPJD(ha, order, checktype, standdetaillist);
            }
            //计算投入产出量
            string[] rr = GetInputAndOutput(order, inoutlist, reflist, parameterlist, ljlcyclic, continuelist);
            DataTable consumedt = tablebll.CreateConsume();
            tablebll.FillConsume(order, ha, ref consumedt);
            tablebll.FillConsume(order, ht, ref consumedt);
            dal.DeleteConsume(order.OrderNo);
            int insert = dal.InsertTable(consumedt);
            string updateordersql = tablebll.GetUpdateOrderSql(order, rr);
            int result = dal.UpdateOrderTime(updateordersql);
            if (order.OrderNo.PadRight(2) == "01")
            {
                dal.UpdateBatchInValue(order.BatchNo, string.IsNullOrEmpty(rr[0]) ? "0" : rr[0]);
            }
            if (order.StageCode == "GYD_JX" || order.StageCode == "GYD_YSB")
            {
                dal.UpdateBatchOutValue(order.BatchNo, string.IsNullOrEmpty(rr[1]) ? "0" : rr[1]);
            }
            IList<OrderModel> temporder = dal.GetUpOrderList(order.BatchNo, order.OrderNo);
            if (temporder.Count > 0)
            {
                IList<InOutConfigModel> temptinlist = dal.GetInoutlist(temporder[0].StageCode, temporder[0].BrandCode);
                string[] rr1 = GetInputAndOutput(temporder[0], temptinlist, reflist, parameterlist, ljlcyclic, continuelist);
                updateordersql = "";
                updateordersql = tablebll.GetUpdateOrderSql(temporder[0], rr1);
                result += dal.UpdateOrderTime(updateordersql);
            }
            if (result > 0)
                logbll.CreateLog(order, "更新工单成功!");
            else
                logbll.CreateLog(order, "更新工单失败!");
        }

        private Hashtable ComputerCPJD(Hashtable ha, OrderModel order, string checktype, IList<StandardDetailModel> standardlist)
        {
            Hashtable ht = new Hashtable();
            try
            {
                foreach (DictionaryEntry e in ha)
                {
                    string parametercode = Convert.ToString(e.Key).Split('|')[0];
                    string macode = Convert.ToString(e.Key).Split('|')[1];
                    string value = Convert.ToString(e.Value);
                    if (macode.Contains("BL"))
                    {
                        string newmacode = macode + "JD";
                        if (standardlist.Count(j => j.ParameterCode == parametercode) > 0)
                        {
                            string center = standardlist.Where(j => j.ParameterCode == parametercode).ToList()[0].CenterValue;
                            if (!string.IsNullOrEmpty(center) && !string.IsNullOrEmpty(value))
                            {
                                if (Convert.ToDouble(center) > 0)
                                {
                                    if (!ht.ContainsKey(parametercode + "|" + newmacode))
                                    {
                                        double v = Math.Abs(Convert.ToDouble(value) - Convert.ToDouble(center)) / Convert.ToDouble(center);
                                        ht.Add(parametercode + "|" + newmacode, v);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logbll.CreateLog(order, "计算批次掺配精度报错：" + e.Message);
            }
            return ht;
        }

        /// <summary>
        /// 获取工单的实际开始时间、实际结束时间
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <param name="startparametercode">工单开始时间参考点</param>
        /// <param name="endparametercode">工单结束时间参考点</param>
        /// <returns>数组索引号0代表开始时间，1代表结束时间</returns>
        public string[] GetOrderRealyTime(OrderModel order, string startparametercode, string endparametercode)
        {
            string[] result = new string[2];
            string pa = "'" + startparametercode + "','" + endparametercode + "'";
            DataTable dt = dal.GetParameterTimeList(order.BatchNo, pa);//根据批次参数编码获取时间列表
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string parametercode = dt.Rows[i]["ParameterCode"].ToString();
                string starttime = dt.Rows[i]["StartTime"].ToString();
                string endtime = dt.Rows[i]["EndTime"].ToString();
                if (parametercode.Equals(startparametercode))
                    result[0] = starttime;
                else if (parametercode.Equals(endparametercode))
                    result[1] = endtime;
            }
            if (string.IsNullOrEmpty(result[0]))
                logbll.CreateLog(order, "工单实际开始时间为空，请检查！");
            if (string.IsNullOrEmpty(result[1]))
                logbll.CreateLog(order, "工单实际结束时间为空，请检查！");
            return result;
        }

        /// <summary>
        /// 计算该工单的掺配量
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <param name="list">掺配配置列表</param>
        /// <param name="reflist">参考点列表</param>
        /// <returns>键值对，key为参数点，value为掺配量</returns>
        public Hashtable GetCPData(OrderModel order, IList<CPConfigModel> list, IList<ProcessRefTag> reflist, IList<ParameterModel> parameterlist, int ljlcyclic, IList<ContinueBatchModel> continuelist)
        {
            Hashtable ha = new Hashtable();
            foreach (CPConfigModel m in list)
            {
                string cptype = m.CPType;
                if (!string.IsNullOrEmpty(m.ParameterCode))
                {
                    string parametercode = m.ParameterCode;
                    string value = "";
                    if (string.IsNullOrEmpty(parametercode))
                        continue;
                    //查询SPC_HISTORY，直接取归集表结果数据不准确，需要将取样周期降低
                    DataTable dt1 = dal.GetParameterTimeList(order.BatchNo, "'" + parametercode + "'");
                    if (dt1.Rows.Count == 0)
                        logbll.CreateLog(order, parametercode + "在归集历史表中不存在结果数据,下一步查找参考点表");
                    else
                    {
                        string starttime = dt1.Rows[0]["StartTime"].ToString();
                        string endtime = dt1.Rows[0]["EndTime"].ToString();
                        if (!string.IsNullOrEmpty(starttime) && !string.IsNullOrEmpty(endtime))
                        {
                            if (parameterlist.Count(j => j.ParameterCode == parametercode) == 0)
                                logbll.CreateLog(order, parametercode + "计算掺配信息时，找不到tag点,下一步查找参考点表");
                            else
                            {
                                string tag = parameterlist.Where(j => j.ParameterCode == parametercode).ToList()[0].HisTag;
                                if (string.IsNullOrEmpty(tag))
                                    logbll.CreateLog(order, parametercode + "计算掺配信息时，找不到tag点");
                                else
                                {
                                    //需先匹配流量点
                                    if (string.IsNullOrEmpty(m.RefParameterCode))
                                    {
                                        logbll.CreateLog(order, parametercode + "计算掺配信息时，找不到依赖的参考点用来判断电子秤");
                                    }
                                    else
                                    {

                                        DataTable dt2 = dal.GetParameterTimeList(order.BatchNo, "'" + m.RefParameterCode + "'");
                                        if (dt2.Rows.Count == 0)
                                            continue;
                                        else
                                        {
                                            string steadystarttime = dt2.Rows[0]["T_StartTime"].ToString();
                                            string steadyendtime = dt2.Rows[0]["T_EndTime"].ToString();
                                            if (string.IsNullOrEmpty(steadystarttime) || string.IsNullOrEmpty(steadyendtime))
                                                continue;
                                            else
                                            {
                                                if (parameterlist.Count(j => j.ParameterCode == m.ParameterCode) == 0)
                                                {
                                                    logbll.CreateLog(order, parametercode + "计算掺配信息时，在基础信息表中找不到参数点");
                                                }
                                                else
                                                {
                                                    string reftag = parameterlist.Where(j => j.ParameterCode == m.RefParameterCode).ToList()[0].HisTag;
                                                    DataTable dt3 = dal.GetHisData(steadystarttime, steadyendtime, reftag, ljlcyclic);
                                                    DataRow[] drs = dt3.Select("vValue<='0'");
                                                    if (dt3.Rows.Count > 0)
                                                    {
                                                        if (Convert.ToDouble(drs.Length) / Convert.ToDouble(dt3.Rows.Count) > 0.9)
                                                        {
                                                            //流量占比超过90%时，视为不走该电子秤
                                                            continue;
                                                        }
                                                        else
                                                        {
                                                            DataTable dt4 = dal.GetHisData(starttime, endtime, tag, ljlcyclic);
                                                            value = GetMax(dt4);
                                                            if (value != "0" && !string.IsNullOrEmpty(value))
                                                            {
                                                                //计算连批累计导致的掺配数据不准
                                                                value = ContinueCP(parametercode, tag, value, order, continuelist, ljlcyclic);
                                                                if (!ha.ContainsKey(parametercode + "|" + cptype))
                                                                    ha.Add(parametercode + "|" + cptype, value);
                                                            }
                                                            continue;
                                                        }
                                                    }
                                                }
                                            }

                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }
            return ha;
        }

        /// <summary>
        /// 判断连批掺配的累计量
        /// </summary>
        /// <param name="parametercode">参数点编码</param>
        /// <param name="tag">his点</param>
        /// <param name="value">当前累计量</param>
        /// <param name="order">当前工单信息</param>
        /// <param name="continuelist">连批信息</param>
        /// <param name="ljlcyclic">取样周期</param>
        /// <returns></returns>
        public string ContinueCP(string parametercode, string tag, string value, OrderModel order, IList<ContinueBatchModel> continuelist, int ljlcyclic)
        {
            if (continuelist.Count(j => j.BatchNo == order.BatchNo && j.StageCode == order.StageCode) == 0)
                return value;
            else
            {
                ContinueBatchModel model = continuelist.Where(j => j.BatchNo == order.BatchNo && j.StageCode == order.StageCode).ToList()[0];
                if (continuelist.Count(j => j.IUID == model.IUID && j.StageCode == order.StageCode && j.SortNo < model.SortNo) == 0)
                    return value;
                else
                {
                    ContinueBatchModel upmodel = continuelist.Where(j => j.IUID == model.IUID && j.StageCode == order.StageCode && j.SortNo < model.SortNo).OrderByDescending(j => j.SortNo).ToList()[0];
                    string upbatchno = upmodel.BatchNo;
                    if (string.IsNullOrEmpty(upbatchno))
                    {
                        logbll.CreateLog(order, parametercode + "计算掺配量时,已存在连批记录，但是批次号为空!");
                        return value;
                    }
                    else
                    {
                        //根据批次号获取上一批该参数点的开始时间和结束时间
                        DataTable dt2 = dal.GetParameterTimeList(upbatchno, "'" + parametercode + "'");
                        if (dt2.Rows.Count == 0)
                        {
                            logbll.CreateLog(order, parametercode + "计算掺配量时,已存在连批记录，前一批:" + upbatchno + "的该参数点在SPC归集表中不存在!");
                            return value;
                        }
                        else
                        {
                            string starttime = dt2.Rows[0]["StartTime"].ToString();
                            string endtime = dt2.Rows[0]["EndTime"].ToString();
                            if (string.IsNullOrEmpty(starttime) || string.IsNullOrEmpty(endtime))
                            {
                                logbll.CreateLog(order, parametercode + "计算掺配量时,已存在连批记录，前一批:" + upbatchno + "的该参数点在SPC归集表中开始时间和结束时间为空!");
                                return value;
                            }
                            //获取实时数据
                            DataTable dt3 = dal.GetHisData(starttime, endtime, tag, ljlcyclic);
                            string maxvalue = GetMax(dt3);
                            string result = (Convert.ToDouble(value) - Convert.ToDouble(maxvalue)).ToString();
                            return result;
                        }
                    }
                }
            }
        }

        public string GetMax(DataTable dt)
        {
            double max = 0;
            if (dt.Rows.Count > 0)
                max = Convert.ToDouble(dt.Rows[0]["vValue"].ToString());
            for (int i = 1; i < dt.Rows.Count; i++)
            {
                if (max < Convert.ToDouble(dt.Rows[i]["vValue"].ToString()))
                    max = Convert.ToDouble(dt.Rows[i]["vValue"].ToString());
            }
            return max.ToString();
        }

        /// <summary>
        /// 计算投入产出
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <param name="outlist">工艺段投入产出信息</param>
        /// <param name="reflist">参考点表信息</param>
        /// <returns>数组，投入量和产出量</returns>
        public string[] GetInputAndOutput(OrderModel order, IList<InOutConfigModel> outlist, IList<ProcessRefTag> reflist, IList<ParameterModel> parameterlist, int ljlcyclic, IList<ContinueBatchModel> continuelist)
        {
            string[] result = new string[2] { "", "" };
            if (outlist.Count(j => j.StageCode == order.StageCode) == 0)
            {
                logbll.CreateLog(order, "计算投入产出量时，无法找到配置信息，请检查！");
            }
            else
            {
                InOutConfigModel outmodel = outlist.Where(j => j.StageCode == order.StageCode && j.LineCode == order.LineCode).ToList()[0];
                string inputcode = outmodel.InputCode;
                string outputcode = outmodel.OutPutCode;
                DataTable dts = dal.GetParameterTimeList(order.BatchNo, "'" + inputcode + "'");//直接在归集表中查找结果数据，投入点
                DataTable dte = dal.GetParameterTimeList(order.BatchNo, "'" + outputcode + "'");//直接在归集表中查找结果数据，产出点
                string inputvalue = "";
                string outputvalue = "";
                //计算投入点
                if (dts.Rows.Count > 0)
                {
                    if (parameterlist.Count(j => j.ParameterCode == inputcode) == 0)
                    {
                        logbll.CreateLog(order, inputcode + "计算投入产出量时，投入参数点，找不到tag点,请检查！");
                    }
                    else
                    {
                        string tag = parameterlist.Where(j => j.ParameterCode == inputcode).ToList()[0].HisTag;
                        string starttime = dts.Rows[0]["StartTime"].ToString();
                        string endtime = dts.Rows[0]["EndTime"].ToString();
                        if (string.IsNullOrEmpty(starttime) || string.IsNullOrEmpty(endtime) || string.IsNullOrEmpty(tag))
                            logbll.CreateLog(order, inputcode + "计算投入量时，找到了开始参考点，但是时间或者tag点为空，请检查!");
                        else
                        {
                            DataTable dt2 = dal.GetHisData(starttime, endtime, tag, ljlcyclic);
                            inputvalue = GetMax(dt2);
                            inputvalue = Continueinout(inputcode, tag, inputvalue, order, continuelist, ljlcyclic);
                            if (string.IsNullOrEmpty(inputvalue) || inputvalue == "0")
                            {
                                for (int i = 0; i < dts.Rows.Count; i++)
                                {
                                    if (inputcode.Equals(dts.Rows[i]["ParameterCode"].ToString()))
                                        inputvalue = dts.Rows[i]["MaxValue"].ToString();
                                }
                            }
                        }

                    }
                }
                if (dte.Rows.Count > 0)
                {
                    if (parameterlist.Count(j => j.ParameterCode == outputcode) == 0)
                    {
                        logbll.CreateLog(order, outputcode + "计算投入产出量时，产出参数点，找不到tag点,请检查！");
                    }
                    else
                    {
                        string tag = parameterlist.Where(j => j.ParameterCode == outputcode).ToList()[0].HisTag;
                        string starttime = dte.Rows[0]["StartTime"].ToString();
                        string endtime = dte.Rows[0]["EndTime"].ToString();
                        if (string.IsNullOrEmpty(starttime) || string.IsNullOrEmpty(endtime) || string.IsNullOrEmpty(tag))
                            logbll.CreateLog(order, outputcode + "计算投入量时，找到了开始参考点，但是时间或者tag点为空，请检查!");
                        else
                        {
                            DataTable dt2 = dal.GetHisData(starttime, endtime, tag, ljlcyclic);
                            outputvalue = GetMax(dt2);
                            outputvalue = Continueinout(outputcode, tag, outputvalue, order, continuelist, ljlcyclic);
                            if (string.IsNullOrEmpty(outputvalue) || outputvalue == "0")
                            {
                                for (int i = 0; i < dte.Rows.Count; i++)
                                {
                                    if (outputcode.Equals(dte.Rows[i]["ParameterCode"].ToString()))
                                        outputvalue = dte.Rows[i]["MaxValue"].ToString();
                                }
                            }
                        }

                    }
                }
                result[0] = inputvalue;
                result[1] = outputvalue;
            }
            return result;
        }

        /// <summary>
        /// 计算连批投入产出量
        /// </summary>
        /// <param name="parametercode">参数点编码</param>
        /// <param name="tag">his点</param>
        /// <param name="value">原始值</param>
        /// <param name="order">工单信息</param>
        /// <param name="continuelist">连批批次信息</param>
        /// <param name="ljlcyclic">取样样本信息</param>
        /// <returns></returns>
        public string Continueinout(string parametercode, string tag, string value, OrderModel order, IList<ContinueBatchModel> continuelist, int ljlcyclic)
        {
            string stagecode = "";
            if (parameterlist.Count(j => j.ParameterCode == parametercode) == 0)
                return "0";
            stagecode = parameterlist.Where(j => j.ParameterCode == parametercode).ToList()[0].StageCode;
            if (continuelist.Count(j => j.BatchNo == order.BatchNo && j.StageCode == stagecode) == 0)
                return value;
            else
            {
                ContinueBatchModel model = continuelist.Where(j => j.BatchNo == order.BatchNo && j.StageCode == stagecode).ToList()[0];
                if (continuelist.Count(j => j.IUID == model.IUID && j.StageCode == stagecode && j.SortNo < model.SortNo) == 0)
                    return value;
                else
                {
                    ContinueBatchModel upmodel = continuelist.Where(j => j.IUID == model.IUID && j.StageCode == stagecode && j.SortNo < model.SortNo).OrderByDescending(j => j.SortNo).ToList()[0];
                    string upbatchno = upmodel.BatchNo;
                    if (string.IsNullOrEmpty(upbatchno))
                    {
                        logbll.CreateLog(order, parametercode + "计算投入产出时,已存在连批记录，但是批次号为空!");
                        return value;
                    }
                    else
                    {
                        //根据批次号获取上一批该参数点的开始时间和结束时间
                        DataTable dt2 = dal.GetParameterTimeList(upbatchno, "'" + parametercode + "'");
                        if (dt2.Rows.Count == 0)
                        {
                            logbll.CreateLog(order, parametercode + "计算投入产出时,已存在连批记录，前一批:" + upbatchno + "的该参数点在SPC归集表中不存在!");
                            return value;
                        }
                        else
                        {
                            string starttime = dt2.Rows[0]["StartTime"].ToString();
                            string endtime = dt2.Rows[0]["EndTime"].ToString();
                            if (string.IsNullOrEmpty(starttime) || string.IsNullOrEmpty(endtime))
                            {
                                logbll.CreateLog(order, parametercode + "计算投入产出时,已存在连批记录，前一批:" + upbatchno + "的该参数点在SPC归集表中开始时间和结束时间为空!");
                                return value;
                            }
                            //获取实时数据
                            DataTable dt3 = dal.GetHisData(starttime, endtime, tag, ljlcyclic);
                            string maxvalue = GetMax(dt3);
                            string result = (Convert.ToDouble(value) - Convert.ToDouble(maxvalue)).ToString();
                            return result;
                        }
                    }
                }
            }

        }

        public string ComputerCSL(OrderModel order)
        {
            //获取以下参数点值
            // GYCS_HSJX_008 JXCLHSL avg
            //GYCS_SSHC_010  SSHCHSL AVG
            //根据批次ID获取第一个工单投入重量
            DataTable dt = dal.GetMainTable(order.BatchNo);
            if (dt.Rows.Count == 0)
            {
                logbll.CreateLog(order, "计算出丝率时，未获取到批次主表信息");
                return "0";
            }
            string input = dt.Rows[0]["PlanValue"].ToString();
            string output = dt.Rows[0]["RealyValue"].ToString();
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
            {
                logbll.CreateLog(order, "计算出丝率时，投入重量或产出重量为空");
                return "0";
            }
            if (Convert.ToDouble(input) == 0 || Convert.ToDouble(output) == 0)
            {
                logbll.CreateLog(order, "计算出丝率时，投入重量或产出重量为0");
                return "0";
            }
            //获取所有的掺配重量
            DataTable cptable = dal.GetCP(order.BatchNo);
            double sum = 0;
            for (int i = 0; i < cptable.Rows.Count; i++)
            {
                if (string.IsNullOrEmpty(cptable.Rows[0]["Value"].ToString()))
                {
                    logbll.CreateLog(order, cptable.Rows[0]["ParameterCode"].ToString() + "计算出丝率时，掺配量为空");
                }
                else
                {
                    sum += Convert.ToDouble(cptable.Rows[i]["Value"].ToString());
                }
            }
            DataTable shtable = dal.GetshParameter(order.BatchNo, "'GYCS_HSJX_008','GYCS_SSHC_010'");
            string sscl = "0";
            string jxcl = "0";
            for (int i = 0; i < shtable.Rows.Count; i++)
            {
                if (shtable.Rows[i]["ParameterCode"].ToString() == "GYCS_HSJX_008")
                {
                    jxcl = shtable.Rows[i]["Avg"].ToString();
                }
                if (shtable.Rows[i]["ParameterCode"].ToString() == "GYCS_SSHC_010")
                {
                    sscl = shtable.Rows[i]["Avg"].ToString();
                }
            }
            if (string.IsNullOrEmpty(sscl) || string.IsNullOrEmpty(jxcl))
            {
                logbll.CreateLog(order, "计算出丝率时，松散回潮出料含水率或者加香出料含水率为空");
                return "0";
            }
            if (Convert.ToDouble(sscl) == 0 || Convert.ToDouble(jxcl) == 0)
            {
                logbll.CreateLog(order, "计算出丝率时，松散回潮出料含水率或者加香出料含水率为0");
                return "0";
            }
            string formcd = "(" + output + "*(" + "100.0-" + jxcl + ")/88)" + "/((" + input + "*(100.0-" + sscl + ")/88)+" + sum + ")";
            logbll.CreateLog(order, "计算出丝率时的公式" + formcd);
            double tt = (Convert.ToDouble(output) * (100.0 - Convert.ToDouble(jxcl)) / 88) / ((Convert.ToDouble(input) * (100.0 - Convert.ToDouble(sscl)) / 88) + sum);
            //double tt = (Convert.ToDouble(output) * (100.0 - Convert.ToDouble(jxcl)) / 88) / ((Convert.ToDouble(input) + sum) * (100.0 - Convert.ToDouble(sscl)) / 88);
            return tt.ToString();
        }
    }
}
