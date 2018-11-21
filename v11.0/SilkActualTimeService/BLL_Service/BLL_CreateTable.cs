using SilkActualTimeService.DAL_GetData;
using SilkActualTimeService.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.BLL_Service
{
    public class BLL_CreateTable
    {
        DAL_Data dal = new DAL_Data();
        /// <summary>
        /// 构造SPC信息表结构
        /// </summary>
        /// <returns></returns>
        public DataTable CreateSPCtable()
        {
            DataTable dt = new DataTable();
            dt.TableName = "SPC_History2";
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Mat_Code", typeof(string));
            dt.Columns.Add("Item_Code", typeof(string));
            dt.Columns.Add("MES_LotCode", typeof(string));
            dt.Columns.Add("Lot_Code", typeof(string));
            dt.Columns.Add("Lot_Year", typeof(string));
            dt.Columns.Add("CPD_Code", typeof(string));
            dt.Columns.Add("GYCS_Code", typeof(string));
            dt.Columns.Add("StandardMaxValue", typeof(double));
            dt.Columns.Add("StandardMinValue", typeof(double));
            dt.Columns.Add("StandardTLValue", typeof(double));
            dt.Columns.Add("AverageValue", typeof(double));
            dt.Columns.Add("MaxValue", typeof(double));
            dt.Columns.Add("MinValue", typeof(double));
            dt.Columns.Add("STD_Deviation1", typeof(double));
            dt.Columns.Add("STD_Deviation2", typeof(double));
            dt.Columns.Add("CPKValue1", typeof(double));
            dt.Columns.Add("CPKValue2", typeof(double));
            dt.Columns.Add("HisSample", typeof(double));
            dt.Columns.Add("MaxCount", typeof(double));
            dt.Columns.Add("MinCount", typeof(double));
            dt.Columns.Add("PassPate", typeof(double));
            dt.Columns.Add("LevelValue", typeof(double));
            dt.Columns.Add("CreateTime", typeof(string));
            dt.Columns.Add("CPKHeadTimes", typeof(float));
            dt.Columns.Add("CPKTailTimes", typeof(float));
            dt.Columns.Add("StopCount", typeof(int));
            dt.Columns.Add("STARTTIME", typeof(string));
            dt.Columns.Add("ENDTIME", typeof(string));
            dt.Columns.Add("Rule_ver", typeof(string));
            dt.Columns.Add("T_StartTime", typeof(string));
            dt.Columns.Add("T_EndTime", typeof(string));
            return dt;
        }

        /// <summary>
        /// 填充SPC信息表
        /// </summary>
        /// <param name="jkbrandcode">集控牌号编码</param>
        /// <param name="brandcode">MES牌号编码</param>
        /// <param name="batchno">MES批次号</param>
        /// <param name="jkbatchno">集控批次号</param>
        /// <param name="year">年份</param>
        /// <param name="productcode">产品段编码</param>
        /// <param name="parametercode">牌号编码</param>
        /// <param name="starttime">全数据开始时间</param>
        /// <param name="endtime">全数据结束时间</param>
        /// <param name="steadystarttime">稳态开始时间</param>
        /// <param name="steadyendtime">稳态结束时间</param>
        /// <param name="result">结果值数组</param>
        /// <param name="dt">SPC表结构信息</param>
        public void FillSPCTable(string jkbrandcode, string brandcode, string batchno, string jkbatchno, string year, string productcode, string parametercode, string starttime, string endtime, string steadystarttime, string steadyendtime, string[] result, ref DataTable dt)
        {
            DataRow dr = dt.NewRow();
            dr["Mat_Code"] = jkbrandcode;
            dr["Item_Code"] = brandcode;
            dr["MES_LotCode"] = batchno;
            dr["Lot_Code"] = jkbatchno;
            dr["CPD_Code"] = productcode;
            dr["GYCS_Code"] = parametercode;
            dr["STARTTIME"] = starttime;
            dr["ENDTIME"] = endtime;
            dr["T_StartTime"] = steadystarttime;
            dr["T_EndTime"] = steadyendtime;
            dr["StandardMaxValue"] = Convert.ToDouble(result[0]);
            dr["StandardMinValue"] = Convert.ToDouble(result[1]);
            dr["StandardTLValue"] = Convert.ToDouble(result[2]);
            dr["AverageValue"] = Convert.ToDouble(result[3]);
            dr["MaxValue"] = Convert.ToDouble(result[4]);
            dr["MinValue"] = Convert.ToDouble(result[5]);
            dr["STD_Deviation1"] = Convert.ToDouble(result[6]);
            dr["STD_Deviation2"] = Convert.ToDouble(result[7]);
            dr["CPKValue1"] = Convert.ToDouble(result[8]);
            dr["CPKValue2"] = Convert.ToDouble(result[9]);
            dr["HisSample"] = Convert.ToDouble(result[10]);
            dr["MaxCount"] = Convert.ToDouble(result[11]);
            dr["MinCount"] = Convert.ToDouble(result[12]);
            dr["PassPate"] = Convert.ToDouble(result[13]);
            dr["LevelValue"] = Convert.ToDouble(result[14]);
            dr["CreateTime"] = result[15];
            dr["CPKHeadTimes"] = Convert.ToDouble("0");
            dr["CPKTailTimes"] = Convert.ToDouble("0");
            dr["StopCount"] = Convert.ToInt32(result[16]);
            dr["Rule_ver"] = Convert.ToDouble(result[17]);
            dt.Rows.Add(dr);
        }

        /// <summary>
        /// 组合更新制丝过程检验请求的结果数据SQL
        /// </summary>
        /// <param name="checktypecode">检验类型</param>
        /// <param name="batchno">批次编码</param>
        /// <param name="productcode">产品段编码</param>
        /// <param name="resultlist">归集结果数据</param>
        /// <returns></returns>
        public string GetCheckUpdateSql(string checktypecode, string batchno, string productcode, IList<CheckResultModel> resultlist,IList<ParameterTimeModel> parametertimelist)
        {
            DataTable zsqurqtable = dal.GetCheck_ZSQURQ(checktypecode, batchno);
            if (zsqurqtable.Rows.Count == 0)
                return "-1";
            string zsqurqid = zsqurqtable.Rows[0]["ZSQURQID"].ToString();
            string productlinecode = zsqurqtable.Rows[0]["PROCESSCD"].ToString();
            DataTable zsqucktable = dal.GetCheck_ZSQUCK(zsqurqid, productcode);
            if (zsqucktable.Rows.Count == 0)
                return "-2";
            string zsquckid = zsqucktable.Rows[0]["zsquckid"].ToString();
            string parametercode = "";
            foreach (CheckResultModel p in resultlist)
            {
                parametercode = parametercode + "'" + p.ParameterCode + "',";
            }
            if (string.IsNullOrEmpty(parametercode))
                return "-3";
            parametercode = parametercode.TrimEnd(',');
            DataTable zsquckpgpatable = dal.GetCheck_ZSQUCKPACELL(zsquckid, parametercode);
            DataTable zspgtable = dal.GetCheck_QUA_ZSQUCKPGPA(zsquckid, parametercode);
            if (zsquckpgpatable.Rows.Count == 0)
                return "-4";
            string updatesql = "";
            for (int i = 0; i < zsquckpgpatable.Rows.Count; i++)
            {
                string pacode = zsquckpgpatable.Rows[i]["ParameterCode"].ToString();
                string prcode = zsquckpgpatable.Rows[i]["ProcessCode"].ToString();
                string indexcode = zsquckpgpatable.Rows[i]["IndexCode"].ToString();
                string indexvalue = zsquckpgpatable.Rows[i]["IndexValue"].ToString();
                string cellid = zsquckpgpatable.Rows[i]["CellID"].ToString();
                if (resultlist.Count(j => j.ParameterCode == pacode && j.ProcessCode == prcode) > 0)
                {
                    CheckResultModel model = resultlist.Where(j => j.ParameterCode == pacode && j.ProcessCode == prcode).ToList()[0];
                    switch (indexcode)
                    {
                        case "Upper_limit":
                            indexvalue =model.array[0]==null?"-9999": model.array[0].ToString();
                            break;
                        case "Lower_limit":
                            indexvalue = model.array[1] == null ? "-9999" : model.array[1].ToString();
                            break;
                        case "Standard":
                            indexvalue = model.array[2] == null ? "-9999" : model.array[2].ToString();
                            break;
                        case "AVG":
                            indexvalue = model.array[3] == null ? "-9999" : model.array[3].ToString();
                            break;
                        case "SD":
                            indexvalue = model.array[7] == null ? "-9999" : model.array[7].ToString();
                            break;
                        case "CPK":
                            indexvalue = model.array[9] == null ? "-9999" : model.array[9].ToString();
                            break;
                        case "CP":
                            indexvalue =model.array[8]== null ? "-9999" : model.array[8].ToString();
                            break;
                        case "CA":
                            indexvalue = "-9999";
                            break;
                        case "MAX":
                            indexvalue = model.array[4] == null ? "0" : model.array[4].ToString();
                            break;
                        case "MIN":
                            indexvalue =model.array[5] == null ? "0" : model.array[5].ToString();
                            break;
                        case "QYS_Result":
                            indexvalue = model.array[13]== null ? "0" : model.array[13].ToString();
                            break;
                        case "Fluctuation":
                            indexvalue = "-9999";
                            break;
                        case "CV":
                            indexvalue = model.array[6]== null ? "-9999" : model.array[6].ToString();
                            break;
                        case "PICKCOUNT":
                            indexvalue = model.array[10] == null ? "0" : model.array[10].ToString();
                            break;
                        case "QUARATE":
                            indexvalue = model.array[13] == null ? "0" : model.array[13].ToString();
                            break;
                        case "QUACOUNT":
                            indexvalue =(Convert.ToInt32(model.array[10] == null ? "0" : model.array[10].ToString()) -Convert.ToInt32(model.array[11]==null ? "0" : model.array[11].ToString()) -Convert.ToInt32(model.array[12] == null ? "0" : model.array[12].ToString())).ToString();
                            break;
                        case "Unqualified_Point":
                            indexvalue = (Convert.ToInt32(model.array[11] == null ? "0" : model.array[11].ToString()) + Convert.ToInt32(model.array[12] == null ? "0" : model.array[12].ToString())).ToString();
                            break;
                        default:
                            break;
                    }
                    updatesql = updatesql + "update QUA_ZSQUCKPACELL set COUNTVAL='" + indexvalue + "' where zsquckpacellid='" + cellid + "';";
                }
            }
            for (int i = 0; i < zspgtable.Rows.Count; i++)
            {
                string pacode = zspgtable.Rows[i]["ParameterCode"].ToString();
                string prcode = zspgtable.Rows[i]["ProcessCode"].ToString();
                string packgaeid = zspgtable.Rows[i]["ZSQUCKPGPAID"].ToString();
                if (resultlist.Count(j => j.ParameterCode == pacode && j.ProcessCode == prcode) > 0)
                {
                    if (parametertimelist.Count(j => j.ProcessCode == prcode && j.ParameterCode == pacode) > 0)
                    {
                        ParameterTimeModel timemodel = parametertimelist.Where(k => k.ProcessCode == prcode && k.ParameterCode == pacode).ToList()[0];
                        updatesql = updatesql + "update QUA_ZSQUCKPGPA set HEADDATETIME='" + timemodel.SteadyStartTime + "',ENDDATETIME='" + timemodel.SteadyEndTime + "' where ZSQUCKPGPAID='" + packgaeid + "'";
                    }
                }
            }
            return updatesql;
        }

        /// <summary>
        /// 构造停机断料表结构
        /// </summary>
        /// <returns></returns>
        public DataTable CreateBatchShutTable()
        {
            DataTable dt = new DataTable("QUA_ZS_BatchShutRecord");
            dt.Columns.Add("ID");
            dt.Columns.Add("BatchNo");
            dt.Columns.Add("OrderNo");
            dt.Columns.Add("ProductLineCode");
            dt.Columns.Add("LineCode");
            dt.Columns.Add("ProductCode");
            dt.Columns.Add("StageCode");
            dt.Columns.Add("ProcessCode");
            dt.Columns.Add("ParameterCode");
            dt.Columns.Add("ShutStartTime");
            dt.Columns.Add("ShutEndTime");
            return dt;
        }

        /// <summary>
        /// 填充停机断料信息表
        /// </summary>
        /// <param name="shutlist">停机断料信息</param>
        /// <param name="dt">停机断料表结构信息</param>
        public void FillBatchShutTable(IList<OrderShutDownModel> shutlist, ref DataTable dt)
        {
            foreach (OrderShutDownModel o in shutlist)
            {
                DataRow dr = dt.NewRow();
                dr["ID"] = Guid.NewGuid().ToString();
                dr["BatchNo"] = o.BatchNo;
                dr["OrderNo"] = o.OrderNo;
                dr["ProductLineCode"] = o.ProductLineCode;
                dr["LineCode"] = o.LineCode;
                dr["ProductCode"] = o.ProductCode;
                dr["StageCode"] = o.StageCode;
                dr["ProcessCode"] = o.ProcessCode;
                dr["ParameterCode"] = o.ParameterCode;
                dr["ShutStartTime"] = o.ShutStartTime;
                dr["ShutEndTime"] = o.ShutEndTime;
                dr["ParameterCode"] = o.ParameterCode;
                dt.Rows.Add(dr);
            }
        }

        /// <summary>
        /// 构造日志信息表结构
        /// </summary>
        /// <returns></returns>
        public DataTable CreateLogtable()
        {
            DataTable dt = new DataTable();
            dt.TableName = "QUA_ZS_BATCHLOG";
            dt.Columns.Add("BatchNo");
            dt.Columns.Add("ProductLineCode");
            dt.Columns.Add("LineCode");
            dt.Columns.Add("ProductCode");
            dt.Columns.Add("StageCode");
            dt.Columns.Add("ProcessCode");
            dt.Columns.Add("LogText");
            dt.Columns.Add("CreateTime");
            dt.Columns.Add("CreateBy");
            return dt;
        }

        /// <summary>
        /// 填充日志信息表
        /// </summary>
        /// <param name="loglist">日志信息列表</param>
        /// <param name="dt">日志信息表</param>
        public void FillLogTable(IList<Batch_Log> loglist, ref DataTable dt)
        {
            foreach (Batch_Log o in loglist)
            {
                DataRow dr = dt.NewRow();
                dr["BatchNo"] = o.BatchNo;
                dr["ProductLineCode"] = o.ProductLineCode;
                dr["LineCode"] = o.LineCode;
                dr["ProductCode"] = o.ProductCode;
                dr["StageCode"] = o.StageCode;
                dr["ProcessCode"] = o.ProcessCode;
                dr["LogText"] = o.LogText;
                dr["CreateBy"] = o.CreateBy;
                dr["CreateTime"] = o.CreateTime;
                dt.Rows.Add(dr);
            }
        }

        /// <summary>
        /// 构建消耗表结构
        /// </summary>
        /// <returns></returns>
        public DataTable CreateConsume()
        {
            DataTable dt = new DataTable("MA_ZS_CONSUME");
            dt.Columns.Add("BatchNo");
            dt.Columns.Add("OrderNo");
            dt.Columns.Add("BrandCode");
            dt.Columns.Add("LineCode");
            dt.Columns.Add("ProductCode");
            dt.Columns.Add("StageCode");
            dt.Columns.Add("MaterielCode");
            dt.Columns.Add("MaterielName");
            dt.Columns.Add("ParameterCode");
            dt.Columns.Add("Value");
            return dt;
        }

        /// <summary>
        /// 填充制丝物耗信息
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <param name="ha">结果数据</param>
        /// <param name="dt">物耗表信息</param>
        public void FillConsume(OrderModel order,Hashtable ha, ref DataTable dt)
        {
            foreach (DictionaryEntry e in ha)
            {
                DataRow dr = dt.NewRow();
                dr["BatchNo"] = order.BatchNo;
                dr["OrderNo"] = order.OrderNo;
                dr["BrandCode"] = order.BrandCode;
                dr["LineCode"] = order.LineCode;
                dr["ProductCode"] = order.ProductCode;
                dr["StageCode"] = order.StageCode;
                dr["MaterielCode"] = Convert.ToString(e.Key).Split('|')[1];
                dr["MaterielName"] = "";
                dr["ParameterCode"] =Convert.ToString(e.Key).Split('|')[0];
                dr["Value"] = e.Value;
                dt.Rows.Add(dr);
            }
        }

        /// <summary>
        /// 获取更新工单的sql语句
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <param name="time">工单真正时间</param>
        /// <param name="value">投入产出</param>
        /// <returns></returns>
        public string GetUpdateOrderSql(OrderModel order,  string[] value)
        {
            string inputvalue =string.IsNullOrEmpty(value[0])?"0": value[0];
            string outputvalue = string.IsNullOrEmpty(value[1]) ? "0" : value[1];
            string sqlstring = "update pl_zs_workorderdetail set IsComputer='2',PlanValue='"+ inputvalue + "',RealyValue='"+ outputvalue + "' where OrderNo='"+order.OrderNo+"'";
            return sqlstring;
        }

        /// <summary>
        /// 构建断料信息表结构
        /// </summary>
        /// <returns></returns>
        public DataTable CreateShutDowntable()
        {
            DataTable dt = new DataTable("QUA_ZS_BatchShutRecord");
            dt.Columns.Add("ID");
            dt.Columns.Add("BatchNo");
            dt.Columns.Add("ProductLineCode");
            dt.Columns.Add("LineCode");
            dt.Columns.Add("ProductCode");
            dt.Columns.Add("StageCode");
            dt.Columns.Add("ProcessCode");
            dt.Columns.Add("ParameterCode");
            dt.Columns.Add("ShutStartTime");
            dt.Columns.Add("ShutEndTime");
            dt.Columns.Add("OrderNo");
            return dt;
        }

        /// <summary>
        /// 填充断料信息
        /// </summary>
        /// <param name="order">工单信息</param>
        /// <param name="list">断料记录</param>
        /// <param name="dt">断料表信息</param>
        public void FillShutDown(OrderModel order,IList<OrderShutDownModel> list, ref DataTable dt)
        {
            foreach (OrderShutDownModel o in list)
            {
                DataRow dr = dt.NewRow();
                dr["ID"] = Guid.NewGuid().ToString();
                dr["BatchNo"] = order.BatchNo;
                dr["ProductLineCode"] = order.ProductLineCode;
                dr["LineCode"] = order.LineCode;
                dr["ProductCode"] = order.ProductCode;
                dr["StageCode"] = order.StageCode;
                dr["ProcessCode"] = o.ProcessCode;
                dr["ParameterCode"] = o.ParameterCode;
                dr["ShutStartTime"] = o.ShutStartTime;
                dr["ShutEndTime"] = o.ShutEndTime;
                dr["OrderNo"] = o.OrderNo;
                dr["ParameterCode"] = o.ParameterCode;
                dt.Rows.Add(dr);
            }
        }

        /// <summary>
        /// 暂存柜信息表结构
        /// </summary>
        /// <returns></returns>
        public DataTable CreateStorageTable()
        {
            DataTable dt = new DataTable();
            dt.TableName = "Meteriel_ZS_Storage";
            dt.Columns.Add("BatchNo");
            dt.Columns.Add("OrderNo");
            dt.Columns.Add("LineCode");
            dt.Columns.Add("StageCode");
            dt.Columns.Add("StorageType");
            dt.Columns.Add("StorageCode");
            dt.Columns.Add("StorageName");
            dt.Columns.Add("InOutFlag");
            dt.Columns.Add("StartTime");
            dt.Columns.Add("EndTime");
            dt.Columns.Add("Value");
            dt.Columns.Add("SurplusValue");
            dt.Columns.Add("CreateTime");
            dt.Columns.Add("CreateBy");
            return dt;
        }

        /// <summary>
        /// 暂存柜信息
        /// </summary>
        /// <param name="list"></param>
        /// <param name="dt"></param>
        public void FillStorageTable(IList<StorageValueModel> list, ref DataTable dt)
        {
            foreach (StorageValueModel s in list)
            {
                DataRow dr = dt.NewRow();
                dr["BatchNo"] = s.BatchNo;
                dr["OrderNo"] = s.OrderNo;
                dr["LineCode"] = s.LineCode;
                dr["StageCode"] = s.StageCode;
                dr["StorageType"] = s.StorageType;
                dr["StorageCode"] = s.StorageCode;
                dr["StorageName"] = s.StorageName;
                dr["InOutFlag"] = s.InOutFlag;
                dr["StartTime"] = s.StartTime;
                dr["EndTime"] = s.EndTime;
                dr["Value"] = s.Value;
                dr["SurplusValue"] = s.SYValue;
                dr["CreateTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                dr["CreateBy"] = "sys";
                dt.Rows.Add(dr);
            }
        }

        public string GetDeleteStorageSQL(IList<StorageValueModel> list)
        {
            string sqlstring = "";
            foreach (StorageValueModel s in list)
            {
                sqlstring += "delete from  Meteriel_ZS_Storage where BatchNo='" + s.BatchNo + "' and StorageCode='" + s.StorageCode + "' and Inoutflag='"+s.InOutFlag+"';";
            }
            return sqlstring;
        }

        public DataTable CreateContinueTable()
        {
            DataTable dt = new DataTable("QUA_ZS_ContinueBatch");
            dt.Columns.Add("PRODUCTLINECODE");
            dt.Columns.Add("LINECODE");
            dt.Columns.Add("PRODUCTCODE");
            dt.Columns.Add("STAGECODE");
            dt.Columns.Add("BATCHCODE");
            dt.Columns.Add("ORDERNO");
            dt.Columns.Add("CREATEBY");
            dt.Columns.Add("CREATETIME");
            dt.Columns.Add("SORTNO");
            dt.Columns.Add("IUID");
            return dt;
        }

        public void FillContinuetable(IList<ContinueBatchModel> list,ref DataTable dt)
        {
            int count = 0;
            foreach (ContinueBatchModel c in list)
            {
                count++;
                DataRow dr = dt.NewRow();
                dr["PRODUCTLINECODE"] = c.ProductLineCode;
                dr["LINECODE"] = c.LineCode;
                dr["PRODUCTCODE"] = c.ProductCode;
                dr["STAGECODE"] = c.StageCode;
                dr["BATCHCODE"] = c.BatchNo;
                dr["ORDERNO"] = "";
                dr["CREATEBY"] = "sys";
                dr["CREATETIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                dr["SORTNO"] = count;
                dr["IUID"] = c.IUID;
                dt.Rows.Add(dr);
            }
        }

        public string GetDeleteSPC(OrderModel order, Hashtable ha,IList<ParameterModel> parameterlist)
        {
            try
            {
                string sqlstring = "delete from SPC_HISTORY2 where MES_LotCode='" + order.BatchNo + "'";
                string[] jugeprocess = ConfigurationManager.AppSettings["jugeprocess"].ToString().Split(';');
                string jugeparameter = ConfigurationManager.AppSettings["jugeparameter"].ToString();
                string parametercode = "";
                bool qlscp = false;
                foreach (DictionaryEntry e in ha)
                {
                    string pa = Convert.ToString(e.Key).Split('|')[0];
                    bool processflag = false;
                    string pg = "";
                    if (parameterlist.Count(j => j.ParameterCode == pa) > 0)
                    {
                        string processcode = parameterlist.Where(j => j.ParameterCode == pa).ToList()[0].ProcessCode;
                        for (int i = 0; i < jugeprocess.Length; i++)
                        {
                            if (processcode.Contains(jugeprocess[i]))
                            {
                                processflag = true;
                                pg = jugeprocess[i];
                                break;
                            }
                        }
                        for (int i = 0; i < jugeprocess.Length; i++)
                        {
                            if (processcode.Contains(jugeprocess[i]) && processcode.Contains("QLSCP"))
                            {
                                qlscp = true;
                                break;
                            }
                        }
                        if (processflag)
                        {
                            foreach (ParameterModel p in parameterlist)
                            {
                                if (p.ProcessCode != processcode && p.ProcessCode.Contains(pg))
                                    parametercode = parametercode + "'" + p.ParameterCode + "',";
                            }
                        }
                    }
                }
                if (!qlscp)
                {
                    foreach (ParameterModel p in parameterlist)
                    {
                        if (p.ProcessCode.Contains("QLSCP"))
                            parametercode = parametercode + "'" + p.ParameterCode + "',";
                    }
                }
                if (!string.IsNullOrEmpty(parametercode))
                {
                    parametercode = parametercode.TrimEnd(',');
                    return sqlstring = sqlstring + " and GYCS_CODE in(" + parametercode + ")";
                }
                else
                    return sqlstring = sqlstring + " and 1<>1";
            }
            catch (Exception e)
            {
                return "99999";
            }
        }

    }
}
