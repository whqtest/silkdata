using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.BLL_Service
{
    public class KPI
    {

        private float[] arr;
        private float USL;
        private float LSL;
        private float SD;
        public KPI()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="array">原始值数据</param>
        /// <param name="USLValue">上线</param>
        /// <param name="LSLValue">下限</param>
        /// <param name="SDValue"><设定值/param>
        public KPI(float[] array, float USLValue, float LSLValue, float SDValue)
        {
            this.arr = array;
            this.USL = USLValue;
            this.LSL = LSLValue;
            this.SD = SDValue;
        }

        /// <summary>
        /// 均值
        /// </summary>
        /// <returns></returns>
        public float AVG()
        {
            try
            {
                float num = this.arr[0];
                for (int index = 1; index < this.arr.Length; ++index)
                    num += this.arr[index];
                return num / (float)this.arr.Length;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 最大值
        /// </summary>
        /// <returns></returns>
        public float MAX()
        {
            float num = 0;
            try
            {
                num = this.arr[0];
                for (int index = 0; index < this.arr.Length; ++index)
                {
                    if ((double)this.arr[index] > (double)num)
                        num = this.arr[index];
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return num;
        }
        /// <summary>
        /// 最小值
        /// </summary>
        /// <returns></returns>
        public float MIN()
        {
            float num = this.arr[0];
            try
            {
                for (int index = 0; index < this.arr.Length; ++index)
                {
                    if ((double)this.arr[index] < (double)num)
                        num = this.arr[index];
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return num;
        }
        /// <summary>
        /// 生产执行能力（CPK）
        /// </summary>
        /// <returns></returns>
        public float CPK()
        {
            float num1 = 0.0f;
            float num2 = this.AVG();
            float num3 = this.STD();
            try
            {
                if ((double)this.SD == 0.0 || (double)this.SD == 99999.0)
                    num1 = 0.0f;
                else if ((double)num3 == 0.0)
                {
                    num1 = 0.0f;
                }
                else
                {
                    float num4 = this.USL;
                    float num5 = this.LSL;
                    if ((double)num4 == 99999.0)
                        num4 = 0.0f;
                    if ((double)num5 == -999.0)
                        num5 = 0.0f;
                    num1 = (float)(((double)num4 - (double)num5 - 2.0 * (double)Math.Abs(this.SD - num2)) / (6.0 * (double)num3));
                }
            }
            catch (Exception ex)
            {
            }
            return num1;
        }

        public float SPCCPK()
        {
            float num1 = 0.0f;
            float num2 = this.AVG();
            float num3 = this.STD();
            try
            {
                num1 = (double)num3 != 0.0 ? ((double)this.USL == 99999.0 || (double)num2 >= (double)this.USL || (double)this.LSL != -999.0 ? ((double)this.LSL == -999.0 || (double)num2 <= (double)this.LSL || (double)this.USL != 99999.0 ? ((double)this.USL == 99999.0 || (double)this.LSL == -999.0 || (double)this.SD == 99999.0 ? 0.0f : (float)(((double)this.USL - (double)this.LSL - 2.0 * (double)Math.Abs(this.SD - num2)) / (6.0 * (double)num3))) : Math.Abs(this.LSL - num2) / (3f * num3)) : Math.Abs(this.USL - num2) / (3f * num3)) : 0.0f;
            }
            catch (Exception ex)
            {
            }
            return num1;
        }

        public float SmallCPK()
        {
            return 0.0f;
        }
        /// <summary>
        /// 标准偏差
        /// </summary>
        /// <returns></returns>
        public float STD()
        {
            try
            {
                if (this.arr.Length <= 1)
                    return this.arr[0];
                float num1 = this.AVG();
                float num2 = (float)(((double)this.arr[0] - (double)num1) * ((double)this.arr[0] - (double)num1));
                for (int index = 1; index < this.arr.Length; ++index)
                    num2 += (float)(((double)this.arr[index] - (double)num1) * ((double)this.arr[index] - (double)num1));
                return Convert.ToSingle(Math.Sqrt((double)num2 / (double)(this.arr.Length - 1)));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public float SmallStd()
        {
            return 0.0f;
        }
        /// <summary>
        /// 计数合格率
        /// </summary>
        /// <returns></returns>
        public float PASS()
        {
            float num1 = 0.0f;
            try
            {
                float num2 = (float)this.arr.Length;
                for (int index = 0; index < this.arr.Length; ++index)
                {
                    if ((double)this.arr[index] <= (double)this.USL && (double)this.arr[index] >= (double)this.LSL)
                        ++num1;
                }
                return (float)((double)num1 / (double)num2 * 100.0);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 合格数
        /// </summary>
        /// <returns></returns>
        public int PassPoint()
        {
            int num = 0;
            try
            {
                for (int index = 0; index < this.arr.Length; ++index)
                {
                    if ((double)this.arr[index] <= (double)this.USL && (double)this.arr[index] >= (double)this.LSL)
                        ++num;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return num;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int AllPoint()
        {
            return this.arr.Length;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int UslPoint()
        {
            int num = 0;
            try
            {
                for (int index = 0; index < this.arr.Length; ++index)
                {
                    if ((double)this.arr[index] > (double)this.USL)
                        ++num;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return num;
        }

        public int LslPoint()
        {
            int num = 0;
            try
            {
                for (int index = 0; index < this.arr.Length; ++index)
                {
                    if ((double)this.arr[index] < (double)this.LSL)
                        ++num;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return num;
        }

        public float Level()
        {
            return 0.0f;
        }
        /// <summary>
        /// 计量合格率
        /// </summary>
        /// <returns></returns>
        public float PercentOfPass()
        {
            float num1 = this.USL;
            float num2 = this.LSL;
            float num3 = (float)Math.Round((double)this.AVG(), 2);
            float num4 = (float)Math.Round((double)this.STD(), 2);
            try
            {
                if ((double)num1 != 99999.0 && (double)num2 == -999.0)
                    return (float)Math.Round(this.NormDistFunc(((double)num1 - (double)num3) / (double)num4) * 100.0, 2);
                if ((double)num1 == 99999.0 && (double)num2 != -999.0)
                    return (float)Math.Round(this.NormDistFunc(((double)num3 - (double)num2) / (double)num4) * 100.0, 2);
                if ((double)num1 != 99999.0 || (double)num2 != -999.0)
                    return (float)Math.Round((this.NormDistFunc(((double)num1 - (double)num3) / (double)num4) - this.NormDistFunc(((double)num2 - (double)num3) / (double)num4)) * 100.0, 2);
                return 9999f;
            }
            catch (Exception ex)
            {
                return 0.0f;
            }
        }

        public float PercentOfPassBLOrJD()
        {
            float num1 = this.USL;
            float num2 = this.LSL;
            float num3 = (float)Math.Round((double)this.AVG(), 4);
            float num4 = (float)Math.Round((double)this.STD(), 4);
            try
            {
                float num5 = (float)Math.Round((this.NormDistFunc(((double)num1 - (double)num3) / (double)num4) - this.NormDistFunc(((double)num2 - (double)num3) / (double)num4)) * 100.0, 4);
                if ((double)num1 != 99999.0 && (double)num2 == -999.0)
                    return (float)Math.Round(this.NormDistFunc(((double)num1 - (double)num3) / (double)num4) * 100.0, 4);
                if ((double)num1 == 99999.0 && (double)num2 != -999.0)
                    return (float)Math.Round(this.NormDistFunc(((double)num3 - (double)num2) / (double)num4) * 100.0, 4);
                if ((double)num1 != 99999.0 || (double)num2 != -999.0)
                    return (float)Math.Round((this.NormDistFunc(((double)num1 - (double)num3) / (double)num4) - this.NormDistFunc(((double)num2 - (double)num3) / (double)num4)) * 100.0, 4);
                return 9999f;
            }
            catch (Exception ex)
            {
                return 0.0f;
            }
        }

        private double f(double x)
        {
            return Math.Exp(-x * x / 2.0) / Math.Sqrt(2.0 * Math.PI);
        }

        private double GetNormSDistValue(float NormSDistValue)
        {
            int num1 = 2;
            double num2 = 0.0;
            double x1 = (double)NormSDistValue;
            while (true)
            {
                double x2 = x1 - (double)num1;
                int index1 = 1;
                double num3 = 0.0 / 1.0;
                double num4 = x1 - x2;
                double num5 = num4 * (this.f(x2) + this.f(x1)) / 2.0;
                double[,] numArray = new double[5000, 5000];
                numArray[1, 1] = num5;
                int index2;
                while (true)
                {
                    int num6 = (int)Math.Pow(2.0, (double)(index1 - 1));
                    if (num6 <= 5000)
                    {
                        num4 /= 2.0;
                        num5 /= 2.0;
                        for (int index3 = 1; index3 <= num6; ++index3)
                            num5 += num4 * this.f(x2 + (double)(2 * index3 - 1) * num4);
                        numArray[index1 + 1, 1] = num5;
                        int num7 = 2 * num6;
                        index2 = 1;
                        while (num7 > 1)
                        {
                            numArray[index1 + 1, index2 + 1] = (Math.Pow(4.0, (double)index2) * numArray[index1 + 1, index2] - numArray[index1, index2]) / (Math.Pow(4.0, (double)index2) - 1.0);
                            num7 /= 2;
                            ++index2;
                        }
                        if (Math.Abs(numArray[index2, index2] - numArray[index2 - 1, index2 - 1]) >= num3)
                            ++index1;
                        else
                            break;
                    }
                    else
                        goto label_3;
                }
                double num8 = numArray[index2, index2];
                num2 += num8;
                if (Math.Abs(num8) >= num3)
                {
                    x1 = x2;
                    num1 = 2 * num1;
                }
                else
                    goto label_14;
            }
        label_3:
            return 0.0;
        label_14:
            return num2;
        }

        private string doubleFormat(float doubleValue)
        {
            try
            {
                string str1 = doubleValue.ToString();
                int length = str1.ToUpper().IndexOf("E");
                if (length == -1)
                    return str1;
                string str2 = str1.Substring(0, length);
                string s = str1.Substring(length + 1);
                if (s.StartsWith("+"))
                    s = s.Substring(1);
                int num = int.Parse(s);
                string format;
                if (num > 0)
                {
                    if (str2.Length - 2 - num > 0)
                    {
                        format = "#.";
                        for (int index = 0; index < str2.Length - 2 - num; ++index)
                            format += (string)(object)0;
                    }
                    else
                        format = "#.0";
                }
                else if (num < 0)
                {
                    format = "0.";
                    for (int index = 0; index < str2.Substring(str2.IndexOf(".") + 1).Length - num; ++index)
                        format += (string)(object)0;
                }
                else
                    format = "#.0";
                if (format.Length == 2)
                    format += (string)(object)0;
                return doubleValue.ToString(format);
            }
            catch (Exception ex)
            {
            }
            return "";
        }

        public float test(float upperLimit, float lowerLimit, float meanValue, float standardDeviation)
        {
            float num;
            try
            {
                num = (float)(this.GetNormSDistValue((upperLimit - meanValue) / standardDeviation) - this.GetNormSDistValue((lowerLimit - meanValue) / standardDeviation)) * 100f;
            }
            catch (Exception ex)
            {
                return 0.0f;
            }
            return (float)Math.Round((double)num, 2);
        }

        private double NormDistFunc(double x)
        {
            double num1 = 1.0 / (1.0 + Math.Abs(x) * 0.2316419);
            double num2 = num1 * (0.31938153 + num1 * (num1 * (1.781477973 + num1 * (num1 * 1.330274429 - 1.821255978)) - 0.356563782));
            double num3 = 1.0 - KPI.norm(x) * num2;
            if (x >= 0.0)
                return num3;
            return 1.0 - num3;
        }

        public static double norm(double x)
        {
            return 1.0 / Math.Sqrt(2.0 * Math.PI) * Math.Exp(-(x * x) / 2.0);
        }

        public  float CP()
        {
            float cz = this.USL - this.LSL;
            float num = this.STD();
            if (num.CompareTo(0) == 0)
            {
                return 0;
            }
            else
            {
                return (float)(cz / (6.0 * num));
            }
        }

        public float CV()
        {
            float num = this.STD();
            float num1 = this.AVG();
            if (num1.CompareTo(0) == 0)
            {
                return 0;
            }
            else
            {
                return (float)(num / num1);
            }
        }
    }


}
