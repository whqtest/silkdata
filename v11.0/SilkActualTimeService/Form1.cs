using SilkActualTimeService.BLL_Service;
using SilkActualTimeService.DAL_GetData;
using SilkActualTimeService.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SilkActualTimeService
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            Init();
        }
        private Thread t1;//丝线归集
        private Thread t2;//梗线归集
        private Thread t3;//暂存柜归集
        private bool flag1 = false;//丝线是否允许结束
        private bool flag2 = false;//梗线是否允许结束
        private bool flag3 = false;//暂存柜是否允许结束
        private int size = Convert.ToInt32(ConfigurationManager.AppSettings["clearsize"].ToString());
        DAL_Data dal = new DAL_Data();
        private void ClearText()
        {
            int count = textBox1.Text.Length;
            if (count >= size)
                textBox1.Clear();
        }
        private void Run(object linecode)
        {
            string linename = "";
            BLL_ZS_ActualTime cbll = new BLL_ZS_ActualTime();
            if ((string)linecode == "GYLX_YX")
                linename = "叶线";
            if ((string)linecode == "GYLX_GX")
                linename = "梗线";
           
            while (true)
            {
                ClearText();
                string dtnow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                textBox1.AppendText(dtnow+":"+linename + "开始计算下一工单\n");
                if ((string)linecode == "GYLX_YX")
                    flag1 = false;
                if ((string)linecode == "GYLX_GX")
                    flag2 = false;
                string r= cbll.Start((string)linecode);
                if ((string)linecode == "GYLX_YX")
                    flag1 = true;
                if ((string)linecode == "GYLX_GX")
                    flag2 = true;
                textBox1.AppendText(r);
                Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["sx_sleep"].ToString()));
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (flag1)
            {
                textBox1.AppendText("每条生产线目前只能开启一个线程，请先关闭该线程\n");
                return;
            }
            t1 = new Thread(new ParameterizedThreadStart(this.Run));
            t1.IsBackground = true;
            t1.Start("GYLX_YX");
        }

        private void StorageRun()
        {
            BLL_ZS_Storage bll = new BLL_ZS_Storage();
            while(true)
            {
                string dtnow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                flag3 = false;
                textBox1.AppendText(dtnow+":暂存柜线程正在运行\n");
                string r= bll.Start();
                textBox1.AppendText(r);
                flag3 = true;
                Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["zcg_sleep"].ToString()));
            }
        }

        private void OrderRun()
        {
            BLL_SilkOrder cbll = new BLL_SilkOrder();
            while (true)
            {
                cbll.WatchOrder("");
                Thread.Sleep(5000);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (flag2)
            {
                textBox1.AppendText("每条生产线目前只能开启一个线程，请先关闭该线程\n");
                return;
            }
            t2 = new Thread(new ParameterizedThreadStart(this.Run));
            t2.IsBackground = true;
            t2.Start("GYLX_GX");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (flag1)
            {
                t1.Abort();
                ClearText();
                textBox1.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "丝线已停止成功！\n");
            }
            else
            {
                ClearText();
                textBox1.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":丝线当前正在计算，请稍后结束！\n");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (flag2)
            {
                t2.Abort();
                ClearText();
                textBox1.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "梗线已停止成功！\n");
            }
            else
            {
                ClearText();
                textBox1.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "梗线当前正在计算，请稍后结束！\n");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            t3 = new Thread(new ThreadStart(this.StorageRun));
            t3.IsBackground = true;
            t3.Start();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (flag3)
            {
                t3.Abort();
                ClearText();
                textBox1.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "暂存柜已停止成功！\n");
            }
            else
            {
                ClearText();
                textBox1.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "暂存柜停止失败！\n");
            }
        }

        private void Init()
        {
            IList<BrandModel> brandlist = dal.GetBrandlist();
            IList<BatchModel> batchlist =new List<BatchModel>();
            IList<ProcessModel> linelist = dal.GetLineList();
            IList<ProcessModel> stagelist = new List<ProcessModel>();
            comboBox1.DataSource = brandlist;
            comboBox1.DisplayMember = "BrandName";
            comboBox1.ValueMember = "BrandCode";
            comboBox2.DataSource = linelist;
            comboBox2.DisplayMember = "LineName";
            comboBox2.ValueMember = "LineCode";
            comboBox3.DataSource = stagelist;
            comboBox3.DisplayMember = "StageName";
            comboBox3.ValueMember = "StageCode";
        }

        private void button7_Click(object sender, EventArgs e)
        {
            BLL_CreateTable tablebll = new BLL_CreateTable();
            string linecode = comboBox2.SelectedValue == null ? "" : ((ProcessModel)comboBox2.SelectedItem).LineCode;
            string brandcode = comboBox1.SelectedValue == null ? "" : ((BrandModel)comboBox1.SelectedItem).BrandCode;
            string stagecode = comboBox3.SelectedValue == null ? "" : ((ProcessModel)comboBox3.SelectedItem).StageCode;
            IList<ContinueBatchModel> list = new List<ContinueBatchModel>();
            string iuid = Guid.NewGuid().ToString();
            for (int i = 0; i < checkedListBox1.CheckedItems.Count; i++)
            {
                ContinueBatchModel model = new ContinueBatchModel();
                model.ProductLineCode = "";
                model.LineCode = linecode;
                model.StageCode = stagecode;
                model.BatchNo= checkedListBox1.CheckedItems[i].ToString();
                model.IUID = iuid;
                model.ProductCode = "";
                list.Add(model);
            }
            IList<ContinueBatchModel> tempclist = list.OrderBy(j => j.BatchNo).ToList();
            DataTable continuetable = tablebll.CreateContinueTable();
            tablebll.FillContinuetable(tempclist, ref continuetable);
            bool flag = dal.CheckContinue(tempclist);
            if (!flag)
            {
                int result = dal.InsertTable(continuetable);
                if(result>0)
                    MessageBox.Show("添加连批成功");
                else
                    MessageBox.Show("添加连批失败");
            }
            else
            {
                MessageBox.Show("某一批已存在连批记录，请检查");
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            IList<ProcessModel> stagelist = dal.GetStageList(((ProcessModel)comboBox2.SelectedItem).LineCode);
            if (comboBox3.Items.Count > 0)
            {
                comboBox3.DataSource = null;
                comboBox3.Items.Clear();
            }
            comboBox3.DataSource = stagelist;
            comboBox3.DisplayMember = "StageName";
            comboBox3.ValueMember = "StageCode";
            checkedListBox1.Items.Clear(); 
            string linecode = comboBox2.SelectedValue == null ? "":((ProcessModel)comboBox2.SelectedItem).LineCode;
            string brandcode= comboBox1.SelectedValue == null ? "" : ((BrandModel)comboBox1.SelectedItem).BrandCode;
            IList<BatchModel> batchlist = dal.GetBatchlist(linecode, brandcode);
            foreach (BatchModel p in batchlist)
            {
                checkedListBox1.Items.Add(p.BatchCode);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
            string linecode = comboBox2.SelectedValue == null ? "" : ((ProcessModel)comboBox2.SelectedItem).LineCode;
            string brandcode = comboBox1.SelectedValue == null ? "" : ((BrandModel)comboBox1.SelectedItem).BrandCode;
            IList<BatchModel> batchlist = dal.GetBatchlist(linecode, brandcode);
            foreach (BatchModel p in batchlist)
            {
                checkedListBox1.Items.Add(p.BatchCode);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.ExitThread();
        }
    }
}
