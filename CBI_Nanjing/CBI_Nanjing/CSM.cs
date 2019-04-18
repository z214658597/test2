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
    public class CSM
    {
        //定义变量
        #region 定义变量区

        #region 轨道电路变量
        Hashtable trackstatehash = new Hashtable();
        String[] trackname = { "dangui_1", "dangui_3", "dangui_5", "dangui_2", "dangui_4", "dangui_6", "daocha_1_1", "daocha_1_3", "Track_G3",
                            "Track_G1","Track_G2","Track_G4","daocha_1_2", "daocha_1_4","dangui_7","dangui_9","dangui_11","dangui_8","dangui_10","dangui_12"};
        public  void trackstateinit()
        {
            for (int i = 0; i < trackname.Length; i++)
            {
                trackstatehash.Add(trackname[i], "0");//初始状态均为占用状态
            }
        }
        #endregion

        #region 信号机变量

        String[] signalname = { "NJN_X", "NJN_XF", "NJN_S3", "NJN_S1", "NJN_S2", "NJN_S4", "NJN_X3", "NJN_X1", "NJN_X2", "NJN_X4", "NJN_SF", "NJN_S" };
        Hashtable Signalstate = new Hashtable();
        public void signalstateinit()
        {
            for (int i = 0; i < signalname.Length; i++)
            {
                Signalstate.Add(signalname[i], "0");//初始状态均为熄灯状态
            }
        }
        String Signal_H;
        String Signal_L;
        String Signal_U1;
        String Signal_U2;
        String Signal_B;
        /*String Xsignal_H;//X进站信号机
        String Xsignal_L;
        String Xsiganl_U1;
        String Xsignal_U2;
        String Xsignal_B;

        String XFsignal_H;//XF进站信号机
        String XFsignal_L;
        String XFsiganl_U1;
        String XFsignal_U2;

        String S3signal_H;//S3出站兼调车信号机
        String S3signal_L;
        String S3siganl_U;
        String S3signal_B;

        String S1signal_H;//S1出站兼调车信号机
        String S1signal_L;
        String S1siganl_U;
        String S1signal_B;

        String S2signal_H;//S2出站兼调车信号机
        String S2signal_L;
        String S2siganl_U;
        String S2signal_B;

        String S4signal_H;//S4出站兼调车信号机
        String S4signal_L;
        String S4siganl_U;
        String S4signal_B;

        String X3signal_H;//X3出站兼调车信号机
        String X3signal_L;
        String X3siganl_U;
        String X3signal_B;

        String X1signal_H;//X1出站兼调车信号机
        String X1signal_L;
        String X1siganl_U;
        String X1signal_B;

        String X2signal_H;//X2出站兼调车信号机
        String X2signal_L;
        String X2siganl_U;
        String X2signal_B;

        String X4signal_H;//X4出站兼调车信号机
        String X4signal_L;
        String X4siganl_U;
        String X4signal_B;

        String Ssignal_H;//S进站信号机
        String Ssignal_L;
        String Ssiganl_U1;
        String Ssignal_U2;

        String SFsignal_H;//XF进站信号机
        String SFsignal_L;
        String SFsiganl_U1;
        String SFsignal_U2;*/
        #endregion

        #region 操作界面按钮变量
        //下行方向

       /* String XnormalStation;//总定位按钮
        String XreverseStation;//总反位按钮
        String XsingleLock;//单锁按钮
        String XsingleUnlock;//单解按钮
        String XLock;//岔封按钮
        String XUnlock;//岔解按钮
        String Xclose;//钮封按钮
        String Xopen;//钮解按钮
        String XrouteCancle;//进路取消按钮
        String XtrkUnlock;//区故解按钮
        String XallUnlock;//引导总锁闭
        String XhandUnlock;//总人解按钮
        String XclearAlarm;//清报警按钮
        String XsignalName;//线号机名称显示按钮
        String XswitchName;//信号机名称显示按钮
        String XtrackName;//信号机名称显示按钮
        //上行方向
        String SnormalStation;//总定位按钮
        String sreverseStation;//总反位按钮
        String SsingleLock;//单锁按钮
        String SsingleUnlock;//单解按钮
        String SLock;//岔封按钮
        String SUnlock;//岔解按钮
        String Sclose;//钮封按钮
        String Sopen;//钮解按钮
        String SrouteCancle;//进路取消按钮
        //String StrackUnlock;//区故解按钮
        String SallUnlock;//引导总锁闭
        String ShandUnlock;//总人解按钮

        String Shelp;//帮助按钮
        String Sguide;//引导接车按钮
        String Svoice;//语音提示
        String Sslopunclock;//坡道解锁
        String SpowerUnlock;//上电解锁*/

        #endregion

        #region 道岔状态采集
        String switch_1;
        String switch_3;
        String switch_2;
        String switch_4;
        #endregion
        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public CSM()
        {
            trackstateinit();
            signalstateinit();
        }
        //采集信息模块：包括轨道电路采集、信号机采集、操作界面按钮、道岔状态信息采集；（分函数分别采集，返回为String类型）；
        #region  轨道电路模块采集
        public StringBuilder TrackCircuit() 
        {
            for (int j = 0; j < 20; j++)             //20个轨道
            {
                if (Drive_Collect.Nanjing_name_CH365_Track[j] != "")
                {
                    Control sk = GetPbControl(Drive_Collect.Nanjing_name_CH365_Track[j]);
                    if (sk == null)
                    {
                       //return;
                    }
                    else
                    {
                        gd_control_show(sk, j);
                    }
                }
            }

            StringBuilder Trackcircuitstr = new StringBuilder();
            foreach (string value in trackstatehash.Values)
            {
               Trackcircuitstr.Append(value+",");
            }
            Trackcircuitstr.Remove(Trackcircuitstr.Length - 1, 1);
            return Trackcircuitstr;
        }

        private void gd_control_show(Control sk, int j)
        {
            if (!sk.InvokeRequired)
            {//1-占用，2-锁闭，3-空闲
                if (sk.Name.Contains("dangui"))
                {
                    if (Drive_Collect.Collect_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Track[j, 0], Drive_Collect.Nanjing_location_CH365_Track[j, 1], Drive_Collect.Nanjing_location_CH365_Track[j, 2]] == 1)
                    {
                      //  ((Track)sk).flag_zt = 1;
                        trackstatehash[sk.Name] = "0";
                    }
                    else
                    {
                        trackstatehash[sk.Name] = "1";
                    }
                   
                }
                else if (sk.Name.Contains("Track"))     //站内股道
                {
                    if (Drive_Collect.Collect_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Track[j, 0], Drive_Collect.Nanjing_location_CH365_Track[j, 1], Drive_Collect.Nanjing_location_CH365_Track[j, 2]] == 1)
                    {
                        trackstatehash[sk.Name] = "0";
                    }
                    else
                    {
                        trackstatehash[sk.Name] = "1";
                    }
                }
                else if (sk.Name.Contains("daocha_1"))
                {
                    if (Drive_Collect.Collect_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Track[j, 0], Drive_Collect.Nanjing_location_CH365_Track[j, 1], Drive_Collect.Nanjing_location_CH365_Track[j, 2]] == 1)
                    {
                        trackstatehash[sk.Name] = "0";
                    }
              
                    else
                        trackstatehash[sk.Name] = "1";
                }
            }
        }
       #endregion


        public StringBuilder Signal()
        {
            foreach (Control ct in CBI_Nanjing.cbi_nanjing.Controls)
            {
                if (ct.Name.Contains("NJN"))
                {
                    int flag = ((Train_Signal)ct).X_flag;
                    String name = ct.Name;
                    switch (flag)
                    {
                        case 2://双黄，1——点亮；0_灯灭
                              Signal_U1="1";
                              Signal_U2="1";
                              Signal_H="0";
                              Signal_L="0";
                              Signal_B="0";
                              Signalstate[ct.Name] = Signal_U1 + Signal_U2 + Signal_H+Signal_L   + Signal_B;
                            break;
                        case 1://单黄
                            Signal_U1="1";
                            Signal_U2="0";
                            Signal_H="0";
                            Signal_L="0";
                            Signal_B="0";
                            Signalstate[ct.Name] = Signal_U1 + Signal_U2 + Signal_H +Signal_L +  Signal_B;
                            break;
                        case 5://红灯
                            Signal_U1="0";
                            Signal_U2="0";
                            Signal_H="1";
                            Signal_L="0";
                            Signal_B="0";
                            Signalstate[ct.Name] = Signal_U1 + Signal_U2 + Signal_H+Signal_L +  Signal_B;
                            break;
                        case 4://白灯
                            Signal_U1="0";
                            Signal_U2= "0";
                            Signal_H= "0";
                            Signal_L="0";
                            Signal_B="1";
                            Signalstate[ct.Name] = Signal_U1 + Signal_U2 + Signal_H+Signal_L+   Signal_B;
                            break;
                        case 3://绿灯
                            Signal_U1 = "0";
                            Signal_U2 = "0";
                            Signal_H = "0";
                            Signal_L = "1";
                            Signal_B = "0";
                            Signalstate[ct.Name] = Signal_U1 + Signal_U2 + Signal_H +Signal_L +  Signal_B;
                            break;
                        case 6://绿黄
                            Signal_U1 = "1";
                            Signal_U2 = "0";
                            Signal_H = "0";
                            Signal_L = "1";
                            Signal_B = "0";
                            Signalstate[ct.Name] = Signal_U1 + Signal_U2 +Signal_H + Signal_L +  Signal_B;
                            break;
                        case 7://红白
                            Signal_U1 = "0";
                            Signal_U2 = "0";
                            Signal_H = "1";
                            Signal_L = "0";
                            Signal_B = "1";
                            Signalstate[ct.Name] = Signal_U1 + Signal_U2 + Signal_H + Signal_L + Signal_B;
                            break;
                    }
                }
                /* case "UU":
                                 ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 2;
                                 break;
                             case "U":
                                 ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 1;
                                 break;
                             case "H":
                                 ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 5;
                                 break;
                             case "B":
                                 ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 4;
                                 break;
                             case "L":
                                 ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 3;
                                 break;
                             case "LH":
                                 ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 6;
                                 break;
                             case "HB":
                                 ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 7;
                                 break;*/
            }

            StringBuilder Siganlstr = new StringBuilder();
            foreach (string value in Signalstate.Values)
            {
                Siganlstr.Append(value+",");
            }
            Siganlstr.Remove(Siganlstr.Length-1,1);
            return Siganlstr;

        }
        public StringBuilder  Operationinterface()//String 
        {
            StringBuilder Operationstr=new StringBuilder();
            for (int i = 0; i < 32; i++)
            {
                Operationstr.Append("1");
            }
                return Operationstr;
        }
        #region 道岔位置采集
        public String Switchstate()//String 
        {
            String SwitchstatesStr=null;
            if (CBI_Nanjing.cbi_nanjing.btn_Daocha1_1.BackColor == Color.Green)
            {
                switch_1 = "1";
            }
            else
            {
                switch_1 = "0";
            }

            if (CBI_Nanjing.cbi_nanjing.btn_Daocha1_2.BackColor == Color.Green)
            {
                switch_2 = "1";
            }
            else
            {
                switch_2 = "0";
            }
            if (CBI_Nanjing.cbi_nanjing.btn_Daocha1_3.BackColor == Color.Green)
            {
                switch_3 = "1";
            }
            else
            {
                switch_3 = "0";
            }
            if (CBI_Nanjing.cbi_nanjing.btn_Daocha1_4.BackColor == Color.Green)
            {
                switch_4 = "1";
            }
            else
            {
                switch_4 = "0";
            }
            SwitchstatesStr = switch_1 +","+ switch_2+"," + switch_3+"," + switch_4;
            return SwitchstatesStr;
        }
        #endregion
        //状态信息整理模块；函数实现，返回String字符串；

        public StringBuilder Informationsummary()//StringBuilder
        {
            StringBuilder track=TrackCircuit();
            StringBuilder signal=Signal();
            StringBuilder operation=Operationinterface();
            String Switch = Switchstate();
            StringBuilder protcol = Communicationprotocol();
            StringBuilder informationsummary = new StringBuilder();
            informationsummary.Append(protcol.ToString()+"#");
            informationsummary.Append(track.ToString()+"#");
            informationsummary.Append(signal.ToString()+"#");
            informationsummary.Append(operation.ToString()+"#");
            informationsummary.Append(Switch.ToString() + "#");
            informationsummary.Append(protcol.ToString());
            return informationsummary;
           // Console.WriteLine(informationsummary.ToString());
        }
        //定义通信协议模块；函数实现返回String类型的字符串
        public StringBuilder Communicationprotocol()//String
        {
            StringBuilder protocol = new StringBuilder("NJN2019ZXL");
            return protocol;
        }



        private Control GetPbControl(string strName)
        {
            string pbName = strName;
            return GetControl(CBI_Nanjing.cbi_nanjing, pbName);
        }
        public static Control GetControl(Control ct, string name)
        {
            Control[] ctls = ct.Controls.Find(name, false);
            if (ctls.Length > 0)
            {
                return ctls[0];
            }
            else
            {
                return null;
            }
        }   
    }
}
