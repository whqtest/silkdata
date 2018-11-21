using SilkActualTimeService.DAL_GetData;
using SilkActualTimeService.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.BLL_Service
{
    public class BLL_ZS_Storage
    {
        private IList<StorageParameterModel> parameterlist = new List<StorageParameterModel>();//存放柜的参数点
        private IList<StorageProcessModel> storageprocesslist = new List<StorageProcessModel>();//存放所有贮叶柜信息
        private IList<JKBrandModel> jkbrandlist = new List<JKBrandModel>();//集控牌号列表
        public static IList<Batch_Log> loglist = new List<Batch_Log>();
        
        BLL_Log logbll = new BLL_Log();
        BLL_CreateTable tablebll = new BLL_CreateTable();
        Hashtable ha = new Hashtable();//用来记录前一贮柜状态信息，key为柜号，value为运行状态
        DAL_Data dal = new DAL_Data();

        public BLL_ZS_Storage()
        {
            parameterlist = dal.GetStorageParameter();
            storageprocesslist = dal.GetSotrage();
        }
        public string Start()
        {
            IList<StorageValueModel> valuelist = new List<StorageValueModel>();
            try
            {
                
                foreach (StorageProcessModel p in storageprocesslist)
                {
                    string batchno = "";
                    int currentstate = -1;
                    if (parameterlist.Count(j => j.LineCode == p.LineCode && j.StageCode == p.StageCode && j.StorageCode == p.StorageCode) == 0)
                    {
                        logbll.CreateStorageLog(p, p.StorageCode + "归集出入柜信息时，状态参数点为空，请检查！");
                        continue;
                    }
                    else
                    {
                        IList<StorageParameterModel> temppalist = parameterlist.Where(j => j.LineCode == p.LineCode && j.StageCode == p.StageCode && j.StorageCode == p.StorageCode).ToList();
                        if (parameterlist.Count(j => j.LineCode == p.LineCode && j.StageCode == p.StageCode && j.StorageCode == p.StorageCode && j.ParameterType == "YXZT") > 0)
                        {
                            StorageParameterModel ztmodel = parameterlist.Where(j => j.LineCode == p.LineCode && j.StageCode == p.StageCode && j.ParameterType == "YXZT" && j.StorageCode == p.StorageCode).ToList()[0];
                            currentstate = CheckState(p, ztmodel.ParameterTag);
                        }
                        else
                        {
                            logbll.CreateStorageLog(p, p.StorageCode + "归集出入柜信息时，找不到出入柜状态点，请检查！");
                            continue;
                        }
                        if (parameterlist.Count(j => j.LineCode == p.LineCode && j.StageCode == p.StageCode && j.StorageCode == p.StorageCode && j.ParameterType == "Batch") > 0)
                        {
                            StorageParameterModel ztmodel = parameterlist.Where(j => j.LineCode == p.LineCode && j.StageCode == p.StageCode && j.ParameterType == "Batch" && j.StorageCode == p.StorageCode).ToList()[0];
                            batchno = CheckBatch(p, ztmodel.ParameterTag);
                        }
                        else
                        {
                            logbll.CreateStorageLog(p, p.StorageCode + "归集出入柜信息时，找不到批次点，请检查！");
                            continue;
                        }
                        if (currentstate == -1)
                        {
                            logbll.CreateStorageLog(p, p.StorageCode + "归集出入柜信息时，解析运行状态错误，请检查！");
                            continue;
                        }
                        else
                        {
                            if (!ha.ContainsKey(p.StorageCode))
                            {
                                ha.Add(p.StorageCode, currentstate.ToString());
                            }
                            else
                            {
                                int type = CheckInout(p, currentstate.ToString(), batchno);
                                GetStorageValue(type, p, batchno, ref valuelist);
                            }
                        }
                    }
                }
                //填充
                DataTable storagetable = tablebll.CreateStorageTable();
                tablebll.FillStorageTable(valuelist, ref storagetable);
                string deletesql = tablebll.GetDeleteStorageSQL(valuelist);
                int del = 0;
                if (!string.IsNullOrEmpty(deletesql))
                    del = dal.DeleteStorage(deletesql);
                if (del != -1)
                {
                    int result = dal.InsertTable(storagetable);
                    if (result > 0)
                        logbll.CreateStorageLog(new StorageProcessModel(), "归集出入柜信息时，插入结果数据成功，请检查！");
                    else
                        logbll.CreateStorageLog(new StorageProcessModel(), "归集出入柜信息时，插入结果数据失败，请检查！");

                }
                else
                    logbll.CreateStorageLog(new StorageProcessModel(), "归集出入柜信息时，删除贮柜数据失败，请检查！");
                DataTable logdt = tablebll.CreateLogtable();
                tablebll.FillLogTable(loglist, ref logdt);
                dal.InsertTable(logdt);//插入日志表
                loglist.Clear();
                valuelist.Clear();
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":等待下一次归集" + "\n";
            }
            catch (Exception e)
            {
                loglist.Clear();
                valuelist.Clear();
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "贮叶柜线程报错" + e.Message+"\n";
            }
          
        }

        /// <summary>
        /// 获取贮柜变化状态
        /// </summary>
        /// <param name="model">贮柜基本信息</param>
        /// <param name="currentstate">当前状态</param>
        /// <param name="batchno">批次号</param>
        /// <returns></returns>
        public int CheckInout(StorageProcessModel model, string currentstate, string batchno)
        {
            int inouttype = 0;
            if (ha.ContainsKey(model.StorageCode))
            {
               
                string upstate = ha[model.StorageCode].ToString();
                if (upstate == "0" && currentstate == "1")
                {
                    //正在进料
                    inouttype = 1;
                }
                else if (upstate == "0" && currentstate == "2")
                {
                    //正在出料
                    inouttype = 2;
                }
                else if (upstate == "0" && currentstate == "3")
                {
                    //边进边出
                    inouttype = 3;
                }
                else if (currentstate == "1" && upstate == "1")
                {
                    //继续进料
                    inouttype = 4;
                }
                else if (currentstate == "2" && upstate == "2")
                {
                    //继续出料
                    inouttype = 5;
                }
                else if (currentstate == "3" && upstate == "3")
                {
                    //继续边进边出
                    inouttype = 6;
                }
                else if (upstate == "1" && currentstate == "4")
                {
                    //进料已经结束
                    inouttype = 7;
                }
                else if (upstate == "4" && currentstate == "4")
                {
                    //当前一直为存料状态
                    inouttype = 10;
                }
                else if (upstate == "2" && currentstate == "0")
                {
                    //出柜完成
                    inouttype = 8;
                }
                else if (upstate == "3" && currentstate == "0")
                {
                    //边进边出完成
                    inouttype = 9;
                }
                if (string.IsNullOrEmpty(batchno))
                {
                    inouttype= - 1;
                }
                ha[model.StorageCode] = upstate;
            }
            return inouttype;
        }

        public void GetStorageValue(int type, StorageProcessModel model, string batchno,ref IList<StorageValueModel> list)
        {
            string instarttime = "";
            string inendtime = "";
            string value = "0";
            string syvalue = "0";
            string orderno = "";
            string outstarttime = "";
            string outendtime = "";
            string instarttag = "";
            string inendtag = "";
            string outstarttag = "";
            string outendtag = "";
            string valuetag = "";
            string ordertag = "";
            string syvaluetag = "";
            string flag = "";
            if (parameterlist.Count(j => j.StorageCode == model.StorageCode && j.ParameterType == "InS") > 0)
            {
                instarttag = parameterlist.Where(j => j.StorageCode == model.StorageCode && j.ParameterType == "InS").ToList()[0].ParameterTag;
            }
            if (parameterlist.Count(j => j.StorageCode == model.StorageCode && j.ParameterType == "InE") > 0)
            {
                inendtag = parameterlist.Where(j => j.StorageCode == model.StorageCode && j.ParameterType == "InE").ToList()[0].ParameterTag;
            }
            if (parameterlist.Count(j => j.StorageCode == model.StorageCode && j.ParameterType == "OutS") > 0)
            {
                outstarttag = parameterlist.Where(j => j.StorageCode == model.StorageCode && j.ParameterType == "OutS").ToList()[0].ParameterTag;
            }
            if (parameterlist.Count(j => j.StorageCode == model.StorageCode && j.ParameterType == "OutE") > 0)
            {
                outendtag = parameterlist.Where(j => j.StorageCode == model.StorageCode && j.ParameterType == "OutE").ToList()[0].ParameterTag;
            }
            if (parameterlist.Count(j => j.StorageCode == model.StorageCode && j.ParameterType == "Order") > 0)
            {
                ordertag = parameterlist.Where(j => j.StorageCode == model.StorageCode && j.ParameterType == "Order").ToList()[0].ParameterTag;
            }
            if (parameterlist.Count(j => j.StorageCode == model.StorageCode && j.ParameterType == "JGValue") > 0)
            {
                valuetag = parameterlist.Where(j => j.StorageCode == model.StorageCode && j.ParameterType == "JGValue").ToList()[0].ParameterTag;
            }
            if (parameterlist.Count(j => j.StorageCode == model.StorageCode && j.ParameterType == "Value") > 0)
            {
                syvaluetag = parameterlist.Where(j => j.StorageCode == model.StorageCode && j.ParameterType == "Value").ToList()[0].ParameterTag;
            }
            orderno = CheckOrder(model, ordertag);
            switch (type)
            {
                case 1:
                    instarttime= CheckStartTime(model, instarttag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "In";
                    StorageValueModel valuemodel1 = new StorageValueModel(model, batchno, orderno, "", instarttime, "", value, syvalue, flag);
                    list.Add(valuemodel1);
                    break;
                case 2:
                    outstarttime = CheckEndTime(model, outstarttag);
                    value= CheckValue(model, valuetag);
                    syvalue= CheckValue(model, syvalue);
                    flag = "Out";
                    value = (Convert.ToDouble(value) - Convert.ToDouble(syvalue)).ToString();
                    StorageValueModel valuemodel2 = new StorageValueModel(model, batchno, orderno, "", outstarttime, "", value, syvalue, flag);
                    list.Add(valuemodel2);
                    break;
                case 3:
                    instarttime = CheckStartTime(model, instarttag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "In";
                    StorageValueModel valuemodel3 = new StorageValueModel(model, batchno, orderno, "", instarttime, "", value, syvalue, flag);
                    list.Add(valuemodel3);
                    outstarttime = CheckStartTime(model, outstarttag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "Out";
                    value = (Convert.ToDouble(value) - Convert.ToDouble(syvalue)).ToString();
                    StorageValueModel valuemodel4 = new StorageValueModel(model, batchno, orderno, "", outstarttime, "", value, syvalue, flag);
                    list.Add(valuemodel4);
                    break;
                case 4:
                    instarttime = CheckStartTime(model, instarttag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "In";
                    StorageValueModel valuemodel5 = new StorageValueModel(model, batchno, orderno, "", instarttime, "", value, syvalue, flag);
                    list.Add(valuemodel5);
                    break;
                case 5:
                    outstarttime = CheckEndTime(model, outstarttag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "Out";
                    value = (Convert.ToDouble(value) - Convert.ToDouble(syvalue)).ToString();
                    StorageValueModel valuemodel6 = new StorageValueModel(model, batchno, orderno, "", outstarttime, "", value, syvalue, flag);
                    list.Add(valuemodel6);
                    break;
                case 6:
                    instarttime = CheckStartTime(model, instarttag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "In";
                    StorageValueModel valuemodel7 = new StorageValueModel(model, batchno, orderno, "", instarttime, "", value, syvalue, flag);
                    list.Add(valuemodel7);
                    outstarttime = CheckStartTime(model, outstarttag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "Out";
                    value = (Convert.ToDouble(value) - Convert.ToDouble(syvalue)).ToString();
                    StorageValueModel valuemodel8 = new StorageValueModel(model, batchno, orderno, "", outstarttime, "", value, syvalue, flag);
                    list.Add(valuemodel8);
                    break;
                case 7:
                    instarttime = CheckStartTime(model, instarttag);
                    inendtime = CheckEndTime(model, inendtag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "In";
                    StorageValueModel valuemodel9 = new StorageValueModel(model, batchno, orderno, "", instarttime, inendtime, value, syvalue, flag);
                    list.Add(valuemodel9);
                    break;
                case 8:
                    outstarttime = CheckStartTime(model, outstarttag);
                    outendtime = CheckEndTime(model, outendtag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "Out";
                    value = (Convert.ToDouble(value) - Convert.ToDouble(syvalue)).ToString();
                    StorageValueModel valuemodel10 = new StorageValueModel(model, batchno, orderno, "", outstarttime, outendtime, value, syvalue, flag);
                    list.Add(valuemodel10);
                    break;
                case 9:
                    instarttime = CheckStartTime(model, instarttag);
                    inendtime = CheckEndTime(model, inendtag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "In";
                    StorageValueModel valuemodel11 = new StorageValueModel(model, batchno, orderno, "", instarttime, "", value, syvalue, flag);
                    list.Add(valuemodel11);
                    outstarttime = CheckStartTime(model, outstarttag);
                    outendtime = CheckEndTime(model, outendtag);
                    value = CheckValue(model, valuetag);
                    syvalue = CheckValue(model, syvalue);
                    flag = "Out";
                    value = (Convert.ToDouble(value) - Convert.ToDouble(syvalue)).ToString();
                    StorageValueModel valuemodel12 = new StorageValueModel(model, batchno, orderno, "", outstarttime, "", value, syvalue, flag);
                    list.Add(valuemodel12);
                    break;
                case 10:
                    //一直为存料状态，需检查数据库中是否存在该数据，如果存在则跳过
                    bool counti = dal.CheckStorage(batchno, model.StorageCode);
                    if (!counti)
                    {
                        instarttime = CheckStartTime(model, instarttag);
                        inendtime = CheckEndTime(model, inendtag);
                        value = CheckValue(model, valuetag);
                        syvalue = CheckValue(model, syvalue);
                        flag = "In";
                        StorageValueModel valuemodel13 = new StorageValueModel(model, batchno, orderno, "", instarttime, "", value, syvalue, flag);
                        list.Add(valuemodel13);
                    }
                    break;
            }
        }

        

        /// <summary>
        /// 将集控牌号转换成MES牌号编码
        /// </summary>
        /// <param name="model">工序信息</param>
        /// <param name="jkbrandcode">集控编码</param>
        /// <returns></returns>
        public string ConvertToMESBrand(StorageProcessModel model, string jkbrandcode)
        {
            jkbrandlist = new List<JKBrandModel>();
            if (jkbrandlist.Count(j => j.JKBrandCode == jkbrandcode) == 0)
            {
                logbll.CreateStorageLog(model, "归集出入柜信息时，找不到集控牌号编码，请检查！");
                return "";
            }
            else
            {
                JKBrandModel jkmodel = jkbrandlist.Where(j => j.JKBrandCode == jkbrandcode).ToList()[0];
                return jkmodel.BrandCode;
            }
        }

        /// <summary>
        /// 检验进出柜状态
        /// </summary>
        /// <param name="histag">进出柜状态数据点</param>
        /// <returns>0空闲，1代表进柜，2代表出柜，3边进边出，4存料</returns>
        public int CheckState(StorageProcessModel model, string histag)
        {
            if (string.IsNullOrEmpty(histag))
            {
                logbll.CreateStorageLog(model, model.StorageCode+"归集出入柜信息时，状态参数点为空，请检查！");
                return -1;
            }
            //根据数据点获取实时数据
            DataTable dt = dal.GetLivData(histag);
            if (dt.Rows.Count > 0)
            {
                string state = dt.Rows[0]["vValue"] == null || string.IsNullOrEmpty(dt.Rows[0].ToString()) ? "-1" : dt.Rows[0]["vValue"].ToString();
                return Convert.ToInt32(state);
            }
            return -1;
        }

        /// <summary>
        /// 获取并检查开始时间
        /// </summary>
        /// <param name="starttimetag">开始时间点</param>
        /// <returns>开始时间</returns>
        public string CheckStartTime(StorageProcessModel model, string starttimetag)
        {
            string rtime = "";
            DataTable dt = dal.GetLivData(starttimetag);
            DataRow[] drs = dt.Select("TagName='" + starttimetag + "'");
            if (string.IsNullOrEmpty(starttimetag))
            {
                logbll.CreateStorageLog(model,model.StorageCode+ "归集出入柜信息,开始时间点为空，请检查！");
                return rtime;
            }
            try
            {
                if (drs.Length > 0)
                {
                    string value = drs[0]["vValue"].ToString();
                    DateTime dts = Convert.ToDateTime(value);
                    rtime = value;
                }
            }
            catch (Exception e)
            {
                logbll.CreateStorageLog(model, model.StorageCode + "归集出入柜信息,开始时间点报错，请检查！" + e.Message);
            }
            return rtime;
        }

        public string CheckOrder(StorageProcessModel model, string ordertag)
        {
            string rtime = "";
            DataTable dt = dal.GetLivData(ordertag);
            DataRow[] drs = dt.Select("TagName='" + ordertag + "'");
            if (string.IsNullOrEmpty(ordertag))
            {
                logbll.CreateStorageLog(model, model.StorageCode + "归集出入柜信息,工单点为空，请检查！");
                return rtime;
            }
            try
            {
                if (drs.Length > 0)
                {
                    string value = drs[0]["vValue"].ToString();
                    rtime = value;
                }
            }
            catch (Exception e)
            {
                logbll.CreateStorageLog(model, model.StorageCode + "归集出入柜信息,报错，请检查！"+e.Message);
            }
            return rtime;
        }

        /// <summary>
        /// 获取并检查结束时间
        /// </summary>
        /// <param name="starttimetag">结束时间点</param>
        /// <returns>结束时间</returns>
        public string CheckEndTime(StorageProcessModel model, string endtimetag)
        {
            string rtime = "";
            DataTable dt = dal.GetLivData(endtimetag);
            DataRow[] dre = dt.Select("TagName='" + endtimetag + "'");
            if (string.IsNullOrEmpty(endtimetag))
            {
                logbll.CreateStorageLog(model, "归集出入柜信息,结束时间点为空，请检查！");
                return rtime;
            }
            try
            {
                if (dre.Length > 0)
                {
                    string value = dre[0]["vValue"].ToString();
                    DateTime dte = Convert.ToDateTime(value);
                    rtime = value;
                }
            }
            catch (Exception e)
            {
                logbll.CreateStorageLog(model, model.StorageCode + "归集出入柜信息,结束时间点报错，请检查！" + e.Message);
            }
            return rtime;
        }

        /// <summary>
        /// 检验批次点信息
        /// </summary>
        /// <param name="model">工序信息</param>
        /// <param name="batchtag">批次点</param>
        /// <returns>批次号</returns>
        public string CheckBatch(StorageProcessModel model, string batchtag)
        {
            if (string.IsNullOrEmpty(batchtag))
            {
                logbll.CreateStorageLog(model, "归集出入柜信息时，批次参数点为空，请检查！");
                return "";
            }
            DataTable dt = dal.GetLivData(batchtag);
            if (dt.Rows.Count > 0)
            {
                string value = dt.Rows[0]["vValue"].ToString();
                return value;
            }
            else
                return "";
        }

        /// <summary>
        /// 检查牌号点
        /// </summary>
        /// <param name="model">工序信息</param>
        /// <param name="brandtag">牌号点</param>
        /// <returns></returns>
        public string CheckBrand(StorageProcessModel model, string brandtag)
        {
            if (string.IsNullOrEmpty(brandtag))
            {
                logbll.CreateStorageLog(model, "归集出入柜信息时，牌号参数点为空，请检查！");
                return "";
            }
            DataTable dt = dal.GetLivData(brandtag);
            if (dt.Rows.Count > 0)
            {
                string value = dt.Rows[0]["vValue"].ToString();
                return value;
            }
            else
                return "";
        }

        /// <summary>
        /// 检查并获取累计值
        /// </summary>
        /// <param name="model">工序信息</param>
        /// <param name="valuetag">累计点</param>
        /// <returns></returns>
        public string CheckValue(StorageProcessModel model, string valuetag)
        {
            if (string.IsNullOrEmpty(valuetag))
            {
                logbll.CreateStorageLog(model, "归集出入柜信息时，牌号参数点为空，请检查！");
                return "0";
            }
            DataTable dt = dal.GetLivData(valuetag);
            if (dt.Rows.Count > 0)
            {
                string value = dt.Rows[0]["vValue"].ToString();
                return value;
            }
            else
                return "0";
        }
    }
}
