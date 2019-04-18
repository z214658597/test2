using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CBI_Nanjing
{
    public partial class PswForm : Form
    {
        private CBI_Nanjing pParent;

        private void frmTopMost_Load(object sender, EventArgs e)
        {
            this.Show();      
        }
       
        public PswForm(CBI_Nanjing pMain)
        {
            InitializeComponent();
            this.ControlBox = false;

            // Init main window object       
            pParent = pMain;
        }

        public PswForm()
        {
            InitializeComponent();
            // TODO: Complete member initialization

        }

        private void btn_0_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = txtBox_Psw.Text + "0";
        }

        private void btn_1_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = txtBox_Psw.Text + "1";
        }

        private void btn_2_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = txtBox_Psw.Text + "2";
        }

        private void btn_3_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = txtBox_Psw.Text + "3";
        }

        private void btn_4_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = txtBox_Psw.Text + "4";
        }

        private void btn_5_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = txtBox_Psw.Text + "5";
        }

        private void btn_6_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = txtBox_Psw.Text + "6";
        }

        private void btn_7_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = txtBox_Psw.Text + "7";
        }

        private void btn_8_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = txtBox_Psw.Text + "8";
        }

        private void btn_9_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = txtBox_Psw.Text + "9";
        }

        private void btn_Sumit_Click(object sender, EventArgs e)
        {
            if (txtBox_Psw.Text == "0109")
            {
                this.Hide();
                MessageBox.Show("已解锁整个站场！");              
            }
            else if (txtBox_Psw.Text=="301"&& ExchangeValue.Xnumber==1&&ExchangeValue.SorX==true)
            {
                ExchangeValue.Xnumber++;
                this.Hide();
            }
            else if (txtBox_Psw.Text == "301" && ExchangeValue.Xnumber % 2 == 0 && ExchangeValue.SorX == true)
            {
                ExchangeValue.Xnumber++;
                CBI_Nanjing.cbi_nanjing.XYinDao();
                this.Hide();
            }
            else if (txtBox_Psw.Text == "301" && ExchangeValue.Xnumber % 3 == 0 && ExchangeValue.SorX == true)
            {
                ExchangeValue.Xnumber=0;
                CBI_Nanjing.cbi_nanjing.XYinDaoCancle();
                this.Hide();
           
            }
            else if (txtBox_Psw.Text == "301" && ExchangeValue.Snumber == 1 && ExchangeValue.SorX == false)
            {
                ExchangeValue.Snumber++;
                this.Hide();
            }
            else if (txtBox_Psw.Text == "301" && ExchangeValue.Snumber % 2 == 0 && ExchangeValue.SorX == false)
            {
                ExchangeValue.Snumber++;
                CBI_Nanjing.cbi_nanjing.SYindao();
                this.Hide();
            }
            else if (txtBox_Psw.Text == "301" && ExchangeValue.Snumber % 3 == 0 && ExchangeValue.SorX == false)
            {
                ExchangeValue.Snumber = 0;
                CBI_Nanjing.cbi_nanjing.SYinDaoCancle();
                this.Hide();

            }

            //区段故障解锁的实现
            else if(txtBox_Psw.Text == "123"&& ExchangeValue.QGnumber==0)
            {
                ExchangeValue.QGnumber++;//第一次按下后对次数进行记录
                ExchangeValue.QG = "区故解";
                CBI_Nanjing.cbi_nanjing.labelshow();
                this.Hide();
            }
            else
            {
                txtBox_Psw.Text = "";
                MessageBox.Show("口令输入有误，请重新输入！");
            }


                               
        }

        private void btn_Cancle_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = "";
            this.Hide();
        }

        private void btn_Delete_Click(object sender, EventArgs e)
        {
            txtBox_Psw.Text = "";
        }
    }
}
