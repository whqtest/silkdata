using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using SilkActualTimeService.Model;
using System.Data.SqlClient;
using System.Data;

namespace SilkActualTimeService.DAL_GetData
{
    public class DAL_Data
    {
        private string myconn = "";
        private string hisconn = "";
        private SqlHelper hishelp = null;
        private SqlHelper meshelp = null;

        public DAL_Data()
        {
            myconn = ConfigurationManager.ConnectionStrings["mesconn"].ConnectionString;
            hisconn = ConfigurationManager.ConnectionStrings["hisconn"].ConnectionString;
            hishelp = new SqlHelper(hisconn);
            meshelp = new SqlHelper(myconn);
        }

        /// <summary>
        /// 获取HIS原始数据
        /// </summary>
        /// <param name="starttime">开始时间</param>
        /// <param name="endtime">结束时间</param>
        /// <param name="ordertag">数据点</param>
        /// <returns></returns>
        public DataTable GetHisData(string starttime, string endtime, string ordertag, int cyclic)
        {
            DataTable dt=new DataTable();
            if (string.IsNullOrEmpty(starttime) || string.IsNullOrEmpty(endtime) || string.IsNullOrEmpty(ordertag))
                return dt;
            string sqlstring = "  SELECT TagName, DateTime, case when Value is null then '0' else Value end as Value,case when vValue is null then '0' else vValue end as vValue,StartDateTime"
                                + " FROM [Runtime].[dbo].History"
                              + " WHERE TagName IN('" + ordertag + "')"
                              + " AND wwRetrievalMode = 'Cyclic'"
                              + " and wwresolution = " + cyclic + " AND wwVersion = 'Latest'  and datetime>='" + starttime + "' and datetime<='" + endtime + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            dt = hishelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }

        /// <summary>
        /// 获取LIVE数据
        /// </summary>
        /// <param name="tag">数据点</param>
        /// <returns></returns>
        public DataTable GetLivData(string tag)
        {
            string sqlstring = " select TagName,DateTime,vValue FROM v_Live where v_Live.TagName IN ('"+tag+"')";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = hishelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }

        /// <summary>
        /// 根据生产线获取参数列表
        /// </summary>
        /// <returns></returns>
        public IList<ParameterModel> GetParameterlist()
        {
            string sqlstring = @" select * from V_ZS_GetParameter";
            //sqlstring+="where t5.cl_code='"+linecode+"'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<ParameterModel>(dt);
        }

        /// <summary>
        /// 获取主要参数点
        /// </summary>
        /// <returns></returns>
        public IList<ProcessMainTag> GetMainParameterlist()
        {
            string sqlstring = @"select t2.*,t1.ParameterType  from QUA_ZS_ProcessMainTag t1 
                               inner join V_GetParameter t2 on t1.GX_CODE=t2.ProcessCode 
                               and t1.GYCS_CODE=t2.ParameterCode ";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<ProcessMainTag>(dt);
        }

        /// <summary>
        /// 获取参考参数点
        /// </summary>
        /// <returns></returns>
        public IList<ProcessRefTag> GetRefParameterlist()
        {
            string sqlstring = @"select t2.*,t1.START_PARAMETERCODE as RefStartParamerterCode,
                                t1.START_HISTAG as RefStartTag,t1.END_PARAMETERCODE as RefEndParameterCode,
                                t1.END_HISTAG as RefEndTag,t1.HISTAG as TagCode,t1.ParameterType,t1.start_offset,t1.end_offset,t1.CutType,t1.startvalue,t1.endvalue
                                 from QUA_ZS_ProcessRefTag t1
                                inner join V_GetParameter t2 on t1.GX_CODE=t2.ProcessCode and t1.GYCS_CODE=t2.ParameterCode ";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<ProcessRefTag>(dt);
        }

        public IList<ProcessModel> GetProcessList()
        {
            string sqlstring = @"select * from V_GetProcess";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<ProcessModel>(dt);
        }

        /// <summary>
        /// 获取截取规则
        /// </summary>
        /// <returns></returns>
        public IList<ProcessCutRuleModel> GetCutRulelist()
        {
            string sqlstring = @"select t2.*,t1.CUTTYPE as Flag,t1.ADDTIME as AddTime,t1.VALUE as Value  from QUA_ZS_CutRule t1 
                                 inner join V_GetParameter t2 on t1.GX_CODE=t2.ProcessCode and t1.GYCS_CODE=t2.ParameterCode";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<ProcessCutRuleModel>(dt);
        }

        public IList<ContinueBatchModel> GetContinuebatchlist()
        {
            string sqlstring = @"select BatchCode as BatchNo,ProductLineCode,LineCode,ProductCode,StageCode,SortNo,IUID from QUA_ZS_ContinueBatch ";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<ContinueBatchModel>(dt);
        }

        public IList<ContinueBatchModel> GetContinuebatchlist(string id)
        {
            string sqlstring = @"select BatchCode as BatchNo,ProductLineCode,LineCode,ProductCode,StageCode,SortNo,IUID from QUA_ZS_ContinueBatch where iuid='"+id+"'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<ContinueBatchModel>(dt);
        }

        public IList<OrderModel> GetOrderList(string linecode)
        {
            //171016400102
            //171115400103
            string sqlstring = @"select top 1  t1.BatchNum as BatchNo,t2.OrderNo, CONVERT(varchar, t2.RealyStartTime,120) as StartTime,
                                CONVERT(varchar, t2.RealyEndTime,120)  as EndTime,t1.SemisCode as BrandCode,t3.* from PL_ZS_WorkOrderMain t1
                                inner join PL_ZS_WorkOrderDetail t2 on t1.IUID=t2.MainID
                                inner join 
                                (select t5.cl_code as LineCode,t5.cl_name as LineName,t4.SECTION_PART_CODE as StageCode,
                                t4.SECTION_PART_NAME as StageName,t6.ProductSegment_code as ProductCode
                                from T_CRAFT_LINE t5
                                inner join T_SECTION_PART t4 on t4.LINE_CODE=t5.cl_code   inner join T_ProductSegment t6 on t6.ProcessSegment_code=t4.SECTION_PART_CODE) t3 on t3.StageCode=t2.SectionCode
                                where t3.LineCode='" + linecode+ "' and  t2.RealyStartTime>='2017-11-29 00:00:00'   and (IsComputer is null or IsComputer=0)  and (t2.RealyStartTime is not null and t2.RealyStartTime<>'' and t2.RealyStartTime<>'1900-01-01 00:00:00.000' and t2.RealyEndTime is not null and t2.RealyEndTime<>'' and t2.RealyEndTime<>'1900-01-01 00:00:00.000')  order by  t2.RealyStartTime";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<OrderModel>(dt);
        }

        public IList<ParameterTimeModel> GetParameterTimelist(string batchcode, string parametercode)
        {
            string sqlstring = "select STARTTIME as StartTime,ENDTIME as EndTime,T_StartTime as SteadyStartTime,T_EndTime as SteadyEndTime"
                              + " from SPC_History2 where MES_LotCode = '" + batchcode + "' and GYCS_Code = '" + parametercode + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<ParameterTimeModel>(dt);
        }

      

        public int UpdateOrder(string orderno,string state)
        {
            string sqlstring = "Update PL_ZS_WorkOrderDetail set  IsComputer='"+state+"' where OrderNo='" + orderno + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            int result = meshelp.ExecuteNonQuery(sqlstring, parameters);
            return result;
        }

        public int Updatezsqurqstate(string zsqurqid)
        {
            string sqlstring = "update   QUA_ZSQURQ set  STATE=2 where ZSQURQID='"+zsqurqid+"'";
            SqlParameter[] parameters = new SqlParameter[] { };
            int result = meshelp.ExecuteNonQuery(sqlstring, parameters);
            return result;
        }

        public int Updatezsquckstate(string zsquckid)
        {
            string sqlstring = "update QUA_ZSQUCK set  STATE=2 where ZSQUCKID='" + zsquckid + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            int result = meshelp.ExecuteNonQuery(sqlstring, parameters);
            return result;
        }

        public int Updatebatchcsl(string batchno, string csl)
        {
            string sqlstring = "Update PL_ZS_WorkOrderMain set  SilkRate='" + csl + "' where BatchNum='" + batchno + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            int result = meshelp.ExecuteNonQuery(sqlstring, parameters);
            return result;
        }

        public DataTable GetCheck_ZSQURQ(string checktype, string batchno)
        {
            string sqlstring = "select top 1 ZSQURQID,PROCESSCD from QUA_ZSQURQ where batchnum='" + batchno + "' and requestcd='" + checktype + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }

        public DataTable GetCheck_ZSQUCK(string zsqurqid, string productcode)
        {
            string sqlstring = "select zsquckid from QUA_ZSQUCK where zsqurqid='" + zsqurqid + "' and prosegmentcd='" + productcode + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }

        public DataTable GetCheck_ZSQUCKPGPA(string zsquckid, string processcode, string parametercode)
        {
            string sqlstring = "select zsquckpgpaid from QUA_ZSQUCKPGPA where zsquckid='" + zsquckid + "' and prostepcd='" + processcode + "' and itemcd='" + parametercode + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }

        public DataTable GetCheck_ZSQUCKPACELL(string zsquckpgpaid, string parametercodes)
        {
            string sqlstring = "select t1.PROSTEPCD as ProcessCode,t1.ITEMCD as ParameterCode,t2.CountCD as IndexCode,t2.zsquckpacellid as CellID,COUNTVAL as IndexValue from QUA_ZSQUCKPGPA t1"
                               + " inner join QUA_ZSQUCKPACELL t2 on t1.ZSQUCKPGPAID = t2.ZSQUCKPGPAID"
                               + " where t1.zsquckid ='" + zsquckpgpaid + "' and t1.ITEMCD in(" + parametercodes + ")";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }

        public DataTable GetCheck_QUA_ZSQUCKPGPA(string zsquckpgpaid, string parametercodes)
        {
            string sqlstring = "select t1.ZSQUCKPGPAID,t1.ITEMCD as ParameterCode, t1.PROSTEPCD as ProcessCode from QUA_ZSQUCKPGPA t1"
                               + " where t1.zsquckid ='" + zsquckpgpaid + "' and t1.ITEMCD in(" + parametercodes + ")";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }

        public DataTable GetCheckZSQUCK(string zsqurqid, string productcode)
        {
            string sqlstring = "select ZSQUCKID from QUA_ZSQUCK where ZSQURQID='" + zsqurqid+ "' and PROSEGMENTCD='"+ productcode + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }

        /// <summary>
        /// 根据牌号获取生产工艺技术标准
        /// </summary>
        /// <param name="brandcode">牌号编码</param>
        /// <returns></returns>
        public IList<StandardMainModel> GetStandardMainlist(string brandcode)
        {
            string sqlstring = "select StandsId as VersionID,BrandCd as BrandCode,[Version] as VersionNo from STD_StandardMain where BrandCd = '" + brandcode + "' and State = 5";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<StandardMainModel>(dt);
        }

        /// <summary>
        /// 根据牌号获取生产工艺技术标准明细
        /// </summary>
        /// <param name="versionid">版本ID</param>
        /// <returns></returns>
        public IList<StandardDetailModel> GetStandardDetaillist(string versionid)
        {
            string sqlstring = @"select t2.ProductLineCd as ProductLineCode,t1.ProductSegmentCd as ProductCode,
                                t1.ProcessSegmentCd as StageCode,t1.ProcessCd as ProcessCode,t1.ParameterCd as ParameterCode,
                                t1.UpperLimit as UpValue,t1.lowerLimit as DownValue,t1.SetValue as CenterValue,IsUpper as IsUp,Islower as IsDown
                                 from STD_StandardMainDetail t1
                                inner
                                 join STD_StandardMain t2 on t1.StandMainId = t2.StandsId";
            sqlstring += " where StandMainId = '" + versionid + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<StandardDetailModel>(dt);
        }

        /// <summary>
        /// 获取检验标准主表
        /// </summary>
        /// <param name="brandcode">牌号编码</param>
        /// <param name="checktype">检验编码</param>
        /// <returns></returns>
        public IList<StandardMainModel> GetCheckStandardMain(string brandcode, string checktype)
        {
            string sqlstring = "select top 1 BrandCd as BrandCode,Standsid as VersionID,[Version] as VersionNo,ProductLinecd as ProductLineCode from STD_CHECKSTANDARDMAIN where brandcd='" + brandcode + "' and State=5 AND checktypecd='"+checktype+"'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<StandardMainModel>(dt);
        }

        /// <summary>
        /// 获取检验标准明细
        /// </summary>
        /// <param name="checkversionid">检验标准ID</param>
        /// <returns></returns>
        public IList<StandardDetailModel> GetCheckStandardDetail(string checkversionid)
        {
            string sqlstring = @"select t2.ProductLineCd as ProductLineCode,t1.ProductSegmentCd as ProductCode,
                                t1.ProcessSegmentCd as StageCode,t1.ProcessCd as ProcessCode,t1.ParameterCd as ParameterCode,
                                t1.UpperLimit as UpValue,t1.lowerLimit as DownValue,t1.SetValue as CenterValue,IsUpper as IsUp,Islower as IsDown
                                 from STD_CHECKSTANDARDDETAIL t1
                                inner
                                 join STD_CHECKSTANDARDMAIN t2 on t1.StandMainId = t2.StandsId";
            sqlstring += " where StandMainId = '" + checkversionid + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<StandardDetailModel>(dt);
        }

        public int UpdateCheck(string sqlstring)
        {
            SqlParameter[] parameters = new SqlParameter[] { };
            return meshelp.ExecuteNonQuery(sqlstring, parameters);
        }

        public int UpdateOrderTime(string sqlstring)
        {
            SqlParameter[] parameters = new SqlParameter[] { };
            return meshelp.ExecuteNonQuery(sqlstring, parameters);
        }

        public int UpdateBatchInValue(string batchid, string value)
        {
            string sqlstring= "update pl_zs_workordermain set planvalue='"+value+"' where batchnum='" + batchid + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            return meshelp.ExecuteNonQuery(sqlstring, parameters);
        }

        public int UpdateBatchOutValue(string batchid, string value)
        {
            string sqlstring = "update pl_zs_workordermain set RealyValue='" + value + "' where batchnum='" + batchid + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            return meshelp.ExecuteNonQuery(sqlstring, parameters);
        }

        public int InsertTable(DataTable dt)
        {
            try
            {
                meshelp.BulkInsert(dt);
                return 1;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public int DeleteSPC(string batchno, string productcode)
        {
            try
            {
                string sqlstring = "delete from SPC_History2 where mes_lotcode='" + batchno + "' and cpd_code='" + productcode + "'";
                SqlParameter[] parameters = new SqlParameter[] { };
                return meshelp.ExecuteNonQuery(sqlstring, parameters);
            }
            catch
            {
                return -1;
            }
        }

        public int DeleteShut(string batchno, string orderno)
        {
            try
            {
                string sqlstring = "delete from QUA_ZS_BatchShutRecord where batchno='" + batchno + "' and orderno='" + orderno + "'";
                SqlParameter[] parameters = new SqlParameter[] { };
                return meshelp.ExecuteNonQuery(sqlstring, parameters);
            }
            catch
            {
                return -1;
            }
        }

        public int DeleteStorage(string sqlstring)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[] { };
                return meshelp.ExecuteNonQuery(sqlstring, parameters);
            }
            catch
            {
                return -1;
            }
        }

        public int DeleteConsume(string orderno)
        {
            string sqlstring = "delete from MA_ZS_CONSUME where OrderNo='"+orderno+"'";
            try
            {
                SqlParameter[] parameters = new SqlParameter[] { };
                return meshelp.ExecuteNonQuery(sqlstring, parameters);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 获取SPCHISTORY数据
        /// </summary>
        /// <param name="batchno">批次编码</param>
        /// <param name="parametercodes">参数列表</param>
        /// <returns></returns>
        public DataTable GetParameterTimeList(string batchno, string parametercodes)
        {
            string sqlstring = "select GYCS_CODE as ParameterCode,StartTime,EndTime,T_StartTime,T_EndTime,MaxValue from SPC_History2 where MES_LotCode='" + batchno + "' and GYCS_Code in(" + parametercodes + ") ";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }

        /// <summary>
        /// 获取工单配置信息
        /// </summary>
        /// <param name="stagecode">工艺段编码</param>
        /// <param name="brandcode">牌号编码</param>
        /// <returns></returns>
        public IList<CPConfigModel> GetConfigList(string stagecode,string brandcode)
        {
            string sqlstring = @"select *
                                 from Meteriel_ZS_CPConifg t1";
              sqlstring += " where t1.StageCode='" + stagecode + "' and BrandCode='"+brandcode+"'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<CPConfigModel>(dt);
        }

        public IList<InOutConfigModel> GetInoutlist(string stagecode, string brandcode)
        {
            string sqlstring = @"select * from Meteriel_ZS_InOutConfig where brandcode='" + brandcode + "' and stagecode='" + stagecode + "'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<InOutConfigModel>(dt);
        }

        public IList<ProcessModel> GetLineList()
        {
            string sqlstring = "select cl_code as LineCode,cl_name as LineName from T_CRAFT_LINE where cl_code like '%GYLX%'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<ProcessModel>(dt);
        }

        public IList<ProcessModel> GetStageList(string linecode)
        {
            string sqlstring = @"   select cl_code as LineCode,cl_name as LineName,t2.SECTION_PART_CODE as StageCode,t2.SECTION_PART_NAME as StageName

                                    from T_CRAFT_LINE t1

                                   inner
                                    join T_SECTION_PART t2 on t1.cl_code = t2.LINE_CODE

                                   where cl_code like '%GYLX%' and t1.cl_code='"+linecode+"'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<ProcessModel>(dt);
        }

        public IList<BatchModel> GetBatchlist(string linecode, string semiscode)
        {
            string sqlstring = @"select  distinct BatchCode from ( select   t1.BatchNum as BatchCode from PL_ZS_WorkOrderMain t1
                                inner join PL_ZS_WorkOrderDetail t2 on t1.IUID=t2.MainID
                                inner join 
                                (select t5.cl_code as LineCode,t5.cl_name as LineName,t4.SECTION_PART_CODE as StageCode,
                                t4.SECTION_PART_NAME as StageName,t6.ProductSegment_code as ProductCode
                                from T_CRAFT_LINE t5
                                inner join T_SECTION_PART t4 on t4.LINE_CODE=t5.cl_code   inner join T_ProductSegment t6 on t6.ProcessSegment_code=t4.SECTION_PART_CODE) t3 on t3.StageCode=t2.SectionCode
                                where t3.LineCode='" + linecode + "' and  t2.RealyStartTime>='2017-11-29 00:00:00' and (IsComputer is null or IsComputer=0)   and t1.SemisCode='" + semiscode + "') tab  order by  BatchCode desc";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<BatchModel>(dt);
        }

        public IList<BrandModel> GetBrandlist()
        {
            string sqlstring = "select semis_code as BrandCode,semis_name as BrandName from  t_semis where is_enable='1' and sscode in('11','12') order by semis_name";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<BrandModel>(dt);
        }

        public bool CheckContinue(IList<ContinueBatchModel> list)
        {
            bool flag = false;
            SqlParameter[] parameters = new SqlParameter[] { };
            foreach (ContinueBatchModel c in list)
            {
                string sqlstring = "select BatchCode from QUA_ZS_ContinueBatch where BatchCode='" + c.BatchNo+"' and StageCode='"+c.StageCode+"'";
                DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
                if (dt.Rows.Count > 0)
                {
                    flag = true;
                    break;
                }
            }
            return flag;
        }

        public bool CheckStorage(string batchno, string storagecode)
        {
            bool flag = false;
            SqlParameter[] parameters = new SqlParameter[] { };
            string sqlstring = "select BatchNo from Meteriel_ZS_Storage where BatchNo='" + batchno + "' and StorageCode='" + storagecode + "'";
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            if (dt.Rows.Count > 0)
                flag = true;
            return flag;
        }

        public IList<StorageParameterModel> GetStorageParameter()
        {
            string sqlstring = "select LineCode,StageCode,StorageCode,StorageName,StorageType,ParameterType,ParameterCode,ParameterTag from Meteriel_ZS_StorageConfig";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<StorageParameterModel>(dt);
        }

        public IList<StorageProcessModel> GetSotrage()
        {
            string sqlstring = "select distinct LineCode,StageCode,StorageCode,StorageName,StorageType from Meteriel_ZS_StorageConfig";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<StorageProcessModel>(dt);
        }

        public IList<OrderModel> GetUpOrderList(string batchno,string orderno)
        {
            //171016400102
            //171115400103
            string sqlstring = @"select top 1  t1.BatchNum as BatchNo,t2.OrderNo, CONVERT(varchar, t2.RealyStartTime,120) as StartTime,
                                CONVERT(varchar, t2.RealyEndTime,120)  as EndTime,t1.SemisCode as BrandCode,t3.* from PL_ZS_WorkOrderMain t1
                                inner join PL_ZS_WorkOrderDetail t2 on t1.IUID=t2.MainID
                                inner join 
                                (select t5.cl_code as LineCode,t5.cl_name as LineName,t4.SECTION_PART_CODE as StageCode,
                                t4.SECTION_PART_NAME as StageName,t6.ProductSegment_code as ProductCode
                                from T_CRAFT_LINE t5
                                inner join T_SECTION_PART t4 on t4.LINE_CODE=t5.cl_code   inner join T_ProductSegment t6 on t6.ProcessSegment_code=t4.SECTION_PART_CODE) t3 on t3.StageCode=t2.SectionCode
                                where t2.Batch_id='" + batchno + "' and t2.OrderNo<'"+orderno+"' and  t2.RealyStartTime>='2017-11-29 00:00:00'   and (IsComputer=3)  and (t2.RealyStartTime is not null and t2.RealyStartTime<>'' and t2.RealyStartTime<>'1900-01-01 00:00:00.000' and t2.RealyEndTime is not null and t2.RealyEndTime<>'' and t2.RealyEndTime<>'1900-01-01 00:00:00.000')  order by  t2.orderno desc";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return SqlHelper.ConvertTo<OrderModel>(dt);
        }

        public DataTable GetMainTable(string batchno)
        {
            string sqlstring = "select PlanValue,RealyValue from PL_zs_workordermain where batchnum='"+batchno+"'";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return dt;

        }

        public DataTable GetCP(string batchno)
        {
            string sqlstring = "select ParameterCode,Value from MA_ZS_CONSUME where batchno='"+batchno+"' and MaterielCode in('BPCP','GSCP','QLSCP','HSCP')";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }

        public DataTable GetshParameter(string batchno, string parametercodes)
        {
            string sqlstring = "select GYCS_CODE as ParameterCode,AverageValue as Avg from SPC_History2 where MES_LotCode='" + batchno + "' and GYCS_Code in(" + parametercodes + ") ";
            SqlParameter[] parameters = new SqlParameter[] { };
            DataTable dt = meshelp.ExecuteDataTable(sqlstring, parameters);
            return dt;
        }
        public DataTable GetArr(string a,string b)
        {
           DataTable dt=new DataTable();
           return dt;
        }
    }
}
