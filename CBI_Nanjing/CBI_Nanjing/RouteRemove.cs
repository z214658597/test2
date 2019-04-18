using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using ControlLib;
namespace CBI_Nanjing
{
    class RouteRemove
    {
        private String Functionbutton;
        private String Startbutton;
       // private CBI_Nanjing pparent=new CBI_Nanjing();
       // public RouteRemove(CBI_Nanjing pMain)
       // {
           // InitializeComponent();
           // this.ControlBox = false;

            // Init main window object       
          //  this.pparent = pMain;
      //  }
        public RouteRemove(String Functionbutton, String Startbutton)
        {
            this.Functionbutton = Functionbutton;
            this.Startbutton = Startbutton;
        }
        public void RouteCancle()
        {

            string Canclebutton = "";//记录进路的终端
            List<string> AJX = new List<string>();//存储除要取消进路始端其余吸起的继电器

            if (((Functionbutton == "btn_XRouteCancle") && (Startbutton == "btn_XJ" || Startbutton == "btn_XFJ" || Startbutton == "btn_S1C" || Startbutton == "btn_S2C" || Startbutton == "btn_S3C" || Startbutton == "btn_S4C" || Startbutton == "btn_XT" || Startbutton == "btn_XFT"))
                || ((Functionbutton == "btn_SRouteCancle") && (Startbutton == "btn_SJ" || Startbutton == "btn_SFJ" || Startbutton == "btn_X1C" || Startbutton == "btn_X2C" || Startbutton == "btn_X3C" || Startbutton == "btn_X4C" || Startbutton == "btn_ST" || Startbutton == "btn_SFT"))) //取消按钮被按下
            {

                foreach (var item in CBI_Nanjing.cbi_nanjing.ht_AJ.Keys)
                {
                    if ((bool)(CBI_Nanjing.cbi_nanjing.ht_AJ[item]) == true && (string)(item) != Startbutton && (string)(item) != "btn_XRouteCancle" && (string)(item) != "btn_SRouteCancle")//
                    {
                        AJX.Add((string)item);//存储除进路始端按钮和取消按钮之外的吸起继电器信息
                    }
                }
                foreach (string st in AJX)
                {
                    if (CBI_Nanjing.cbi_nanjing.ht_InterlockingTable.Contains(Startbutton + "+" + st))//此处存在按下的是终端按钮时同样可以对应到联锁表
                    {
                        Canclebutton = st;
                    }
                }
                if (Canclebutton == "")//无终端按钮，无进路
                {
                    MessageBox.Show("请先办理进路");
                    //按钮恢复
                    foreach (string st in CBI_Nanjing.cbi_nanjing.list_button_Route)
                    {
                        foreach (Control ct in CBI_Nanjing.cbi_nanjing.Controls)
                        {
                            if (ct.Name == st)
                            {
                                ((Button)ct).Enabled = true;
                                //  ((Button)ct).BackColor = Color.Green;
                                if (ct.Name == "btn_XT" || ct.Name == "btn_XFT" || ct.Name == "btn_ST" || ct.Name == "btn_SFT")
                                {
                                    ((Button)ct).BackColor = Color.Blue;
                                }
                                else
                                {
                                    ((Button)ct).BackColor = Color.Green;

                                }
                            }
                        }

                    }
                    CBI_Nanjing.cbi_nanjing.list_button_Route.Clear();//把按钮存的信息清理
                    CBI_Nanjing.cbi_nanjing.ht_AJ[Startbutton] = false;//恢复按钮继电器的状态

                }//null的判断
                else
                {
                    if ((Startbutton == "btn_XJ" || Startbutton == "btn_XFJ" || Startbutton == "btn_SJ" || Startbutton == "btn_SFJ") && ((int)(CBI_Nanjing.cbi_nanjing.ht_FXJ[Startbutton]) == 1)) //接车取消
                    {
                        CBI_Nanjing.cbi_nanjing.ht_AJ[Startbutton] = false;//按钮继电器恢复
                        if (Canclebutton != "")
                        {
                            CBI_Nanjing.cbi_nanjing.ht_AJ[Canclebutton] = false;
                        }


                        Startbutton = Startbutton + "+" + Canclebutton;
                        string info = (string)(CBI_Nanjing.cbi_nanjing.ht_InterlockingTable[Startbutton]);//找到对应的联锁表信息
                        string xhj = info.Split('#')[0];
                        string xhj2 = xhj.Split('|')[0];

                        //通过信号机找到接近区段
                        string jtrack = (string)CBI_Nanjing.cbi_nanjing.ht_SingalandTrack[xhj2];//根据信号机找到对应的接近区段的名称


                        string dc = info.Split('#')[1];
                        string dg = info.Split('#')[2];
                        {
                            if (((Track)CBI_Nanjing.cbi_nanjing.ht_Track[jtrack]).flag_zt == 3)//1==占用，2==锁闭 3==空闲
                            {
                                CBI_Nanjing.cbi_nanjing.RouteCancle_Mark(xhj, dc, dg);
                                foreach (string t in CBI_Nanjing.cbi_nanjing.list_InterlockingTable)
                                {
                                    if (t == info)
                                    {
                                        CBI_Nanjing.cbi_nanjing.list_InterlockingTable.Remove(t);    //移除进路信息
                                        break;
                                    }
                                }

                                foreach (var item in CBI_Nanjing.cbi_nanjing.list_InterlockingTable)
                                {
                                    CBI_Nanjing.cbi_nanjing.CBItoRBCRoutemsg(item);//此处向RBC更新进路的信息
                                }
                                //按钮恢复正常
                                foreach (string item in CBI_Nanjing.cbi_nanjing.list_button_Route)
                                {
                                    foreach (Control ct in CBI_Nanjing.cbi_nanjing.Controls)//效率很慢
                                    {
                                        if (ct.Name == item)
                                        {
                                            ((Button)ct).Enabled = true;
                                            // ((Button)ct).BackColor = Color.Green;
                                            if (ct.Name == "btn_XT" || ct.Name == "btn_XFT" || ct.Name == "btn_ST" || ct.Name == "btn_SFT")
                                            {
                                                ((Button)ct).BackColor = Color.Blue;
                                            }
                                            else
                                            {
                                                ((Button)ct).BackColor = Color.Green;

                                            }
                                        }

                                    }

                                }//foreach
                                CBI_Nanjing.cbi_nanjing.list_button_Route.Clear();
                                CBI_Nanjing.cbi_nanjing.RouteNum--;
                                CBI_Nanjing.cbi_nanjing.ht_FXJ[Startbutton.Split('+')[0]] = 0;

                            }//判断接近区段的情况

                        }//string下的

                    }//if
                    else if ((Startbutton == "btn_X1C" || Startbutton == "btn_X2C" || Startbutton == "btn_X3C" || Startbutton == "btn_X4C" || Startbutton == "btn_S1C"
                        || Startbutton == "btn_S2C" || Startbutton == "btn_S3C" || Startbutton == "btn_S4C") && ((int)(CBI_Nanjing.cbi_nanjing.ht_FXJ[Startbutton]) == -1))//发车取消
                    {
                        CBI_Nanjing.cbi_nanjing.ht_AJ[Startbutton] = false;//按钮继电器恢复
                        if (Canclebutton != "")
                        {
                            CBI_Nanjing.cbi_nanjing.ht_AJ[Canclebutton] = false;
                        }
                        Startbutton = Startbutton + "+" + Canclebutton;
                        string info = (string)(CBI_Nanjing.cbi_nanjing.ht_InterlockingTable[Startbutton]);
                        string xhj = info.Split('#')[0];
                        string dc = info.Split('#')[1];
                        string dg = info.Split('#')[2];
                        {
                            CBI_Nanjing.cbi_nanjing.RouteCancle_Mark(xhj, dc, dg);

                            foreach (string t in CBI_Nanjing.cbi_nanjing.list_InterlockingTable)
                            {
                                if (t == info)
                                {
                                    CBI_Nanjing.cbi_nanjing.list_InterlockingTable.Remove(t);    //移除进路信息
                                    break;
                                }
                            }

                            foreach (var item in CBI_Nanjing.cbi_nanjing.list_InterlockingTable)
                            {
                                CBI_Nanjing.cbi_nanjing.CBItoRBCRoutemsg(item);//更新进路信息
                            }
                            //按钮恢复正常
                            foreach (string item in CBI_Nanjing.cbi_nanjing.list_button_Route)
                            {
                                foreach (Control ct in CBI_Nanjing.cbi_nanjing.Controls)//效率很慢
                                {
                                    if (ct.Name == item)
                                    {
                                        ((Button)ct).Enabled = true;
                                        // ((Button)ct).BackColor = Color.Green;
                                        if (ct.Name == "btn_XT" || ct.Name == "btn_XFT" || ct.Name == "btn_ST" || ct.Name == "btn_SFT")
                                        {
                                            ((Button)ct).BackColor = Color.Blue;
                                        }
                                        else
                                        {
                                            ((Button)ct).BackColor = Color.Green;

                                        }
                                    }

                                }

                            }//foreach
                            CBI_Nanjing.cbi_nanjing.list_button_Route.Clear();
                            CBI_Nanjing.cbi_nanjing.RouteNum--;
                            CBI_Nanjing.cbi_nanjing.ht_FXJ[Startbutton.Split('+')[0]] = 0;


                        }//string下的
                    }//else下的
                    else if ((Startbutton == "btn_XT" || Startbutton == "btn_XFT" || Startbutton == "btn_ST" || Startbutton == "btn_SFT" && ((int)(CBI_Nanjing.cbi_nanjing.ht_FXJ[Startbutton]) == 2)))
                    {
                        //通过进路的取消
                        CBI_Nanjing.cbi_nanjing.ht_AJ[Startbutton] = false;//按钮继电器恢复
                        if (Canclebutton != "")
                        {
                            CBI_Nanjing.cbi_nanjing.ht_AJ[Canclebutton] = false;
                        }
                        Startbutton = Startbutton + "+" + Canclebutton;
                        string info = (string)(CBI_Nanjing.cbi_nanjing.ht_InterlockingTable[Startbutton]);
                        string xhj = info.Split('#')[0];
                        string dc = info.Split('#')[1];
                        string dg = info.Split('#')[2];
                        {

                            CBI_Nanjing.cbi_nanjing.RouteCancle_Mark(xhj, dc, dg);
                            foreach (string t in CBI_Nanjing.cbi_nanjing.list_InterlockingTable)
                            {
                                if (t == info)
                                {
                                    CBI_Nanjing.cbi_nanjing.list_InterlockingTable.Remove(t);    //移除进路信息
                                    break;
                                }
                            }

                            foreach (var item in CBI_Nanjing.cbi_nanjing.list_InterlockingTable)
                            {
                                CBI_Nanjing.cbi_nanjing.CBItoRBCRoutemsg(item);//更新进路信息
                            }
                            //按钮恢复正常
                            foreach (string item in CBI_Nanjing.cbi_nanjing.list_button_Route)
                            {
                                foreach (Control ct in CBI_Nanjing.cbi_nanjing.Controls)//效率很慢
                                {
                                    if (ct.Name == item)
                                    {
                                        ((Button)ct).Enabled = true;
                                        //  ((Button)ct).BackColor = Color.Green;
                                        if (ct.Name == "btn_XT" || ct.Name == "btn_XFT" || ct.Name == "btn_ST" || ct.Name == "btn_SFT")
                                        {
                                            ((Button)ct).BackColor = Color.Blue;
                                        }
                                        else
                                        {
                                            ((Button)ct).BackColor = Color.Green;

                                        }
                                    }

                                }

                            }//foreach
                            CBI_Nanjing.cbi_nanjing.list_button_Route.Clear();
                            CBI_Nanjing.cbi_nanjing.RouteNum--;
                            CBI_Nanjing.cbi_nanjing.ht_FXJ[Startbutton.Split('+')[0]] = 0;
                        }//string下的
                    }
                    else
                    {
                        MessageBox.Show("进路取消请按取消按钮+进路始端按钮");
                        //按钮恢复
                        foreach (string item in CBI_Nanjing.cbi_nanjing.list_button_Route)
                        {
                            foreach (Control ct in CBI_Nanjing.cbi_nanjing.Controls)//效率很慢
                            {
                                if (ct.Name == item)
                                {
                                    ((Button)ct).Enabled = true;
                                    // ((Button)ct).BackColor = Color.Green;
                                    if (ct.Name == "btn_XT" || ct.Name == "btn_XFT" || ct.Name == "btn_ST" || ct.Name == "btn_SFT")
                                    {
                                        ((Button)ct).BackColor = Color.Blue;
                                    }
                                    else
                                    {
                                        ((Button)ct).BackColor = Color.Green;
                                    }
                                }

                            }

                        }//foreach
                        // ht_AJ[secondbutton] = false;
                        CBI_Nanjing.cbi_nanjing.list_button_Route.Clear();
                    }
                }//else
            }//进路取消按钮条件进入
        }
    }
}



 
