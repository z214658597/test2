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
/// <summary>
/// 所有自定义控件，来源于ControlLib：
/// 信号机：Xinhaoji2
/// 单动道岔：Daocha_1
/// 双动道岔：Daocha_2
/// 轨道：Dangui
/// 指示灯：Danniu
/// </summary>

namespace CBI_Nanjing
{
    public partial class CBI_Nanjing : Form
    {
        #region  界面调用声明单例化模式
        public static CBI_Nanjing cbi_nanjing;
        #endregion

        #region 自适应屏幕分辨率
        AutoSizeFormClass asc = new AutoSizeFormClass();

        private void CBI_Nanjing_SizeChanged(object sender, EventArgs e)
        {
            asc.controlAutoSize(this);           
        }
        #endregion

        #region 存储站场布置数据变量
        public List<string> list_button_Route = new List<string>();              //链表进路按钮
        List<String> list_Xbutton_Switch = new List<string>();     //链表下行咽喉道岔单操按钮
        List<String> list_Sbutton_Switch = new List<string>();     //链表上行咽喉道岔单操按钮
        List<Image> list_button_bmp = new List<Image>();             //链表按键图片
        List<Thread> list_Thread = new List<Thread>();          //链表线程
        public List<string> list_InterlockingTable = new List<string>();          //链表联锁表
        List<Track> list_Section = new List<Track>();            //链表区间占用信息

        public Hashtable ht_Signal = new Hashtable();    //哈希表信号机（实时检测结果）
        public Hashtable ht_Track = new Hashtable();     //哈希表轨道（实时检测结果）
        public Hashtable ht_SwitchState = new Hashtable();//道岔状态记录（用于单操）
        public Hashtable ht_SingleActingSwitch = new Hashtable();    //哈希表单动道岔 （实时检测结果）
        public Hashtable ht_InterlockingTable = new Hashtable();     //哈希表联锁表
        public  Hashtable ht_AJ = new Hashtable();//哈希表按钮继电器，记录按钮是否被按下 string bool
        public Hashtable ht_FXJ = new Hashtable();//哈希表方向继电器
        public Hashtable ht_TrainInterlockingInfo = new Hashtable();  //哈希表列车联锁信息
        public Hashtable ht_TrainTrackInfo = new Hashtable();  //哈希表列车股道信息
        public Hashtable ht_SingalandTrack = new Hashtable();//接近区段与信号机的联系

        Thread Thread_Collect, Thread_Dianniu_Display;
        bool flag = false;   //模式转换的标记CTC
        bool flagRBC = false;//作为与RBC通信是的判断条件
       // bool LJJ = false;//方向继电器列车接车方向
       // bool LFJ = false;//方向继电器列车发车方向
      //  bool flagJYJ = false;//作为总人解倒计时完成的标志
       

        private int btn_Signal_Count = 0;
        private int btn_Switch_Count = 0;
        private int btn_Track_Count = 0;  //三大信号设备名称显示按钮点击记录次数
        public int RouteNum = 0; //记录进路的条数

        private int btn_Help_Count = 0;   //辅助按钮点击记录次数

        private PswForm myTopMost;   //悬浮的保护口令窗口

        RouteRemove routeremove;//定义针对总人解的取消进路类
        #endregion

        #region 站场模式初始状态指示灯
        class CBI_state
        {
            public static bool CTC_control_mode = false;
            public static bool Self_control_mode = true;
            public static bool Self_to_CTC_mode = true;
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public CBI_Nanjing()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            cbi_nanjing = this;
        }
        #endregion

        #region 通信连接部分

        #region 服务器端程序
        public string Server_Rec;  //作为服务器接收到的信息  

        Thread ThreadofServer = null;
        Socket SocketofServer = null;  //分别创建1个监听客户端的线程和套接字              

        Dictionary<string, Socket> DictSocket = new Dictionary<string, Socket>(); //保存服务器端所有负责和客户端通信的套接字    
        string Client_Name;  //被访问客户端的名称
        IPAddress Client_IP;  //被访问客户端的IP
        int Client_Port;   //被访问客户端的端口号   

        private void CBI服务器建立ToolStripMenuItem_Click(object sender, EventArgs e)  //建立本地服务器
        {
            //在多线程程序中,新创建的线程不能访问UI线程创建的窗口控件,这个时候如果你想要访问窗口的控件,那么你可以将窗口构造函数
            //中的CheckForIllegalCrossThreadCalls设置为false.这时线程就能安全的访问窗体控件了
            CheckForIllegalCrossThreadCalls = false;  

            //定义一个套接字用于监听客户端发来的信息，包含3个参数(IPV4寻址协议,流式连接,TCP协议)
            SocketofServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //发送信息需要1个IP地址和端口号           
            IPAddress IP_Address = GetLocalIPv4Address();   //获取服务端IPv4地址                
            int IP_Port = 65535;   //给服务端赋予一个端口号         

            IPEndPoint Endpoint = new IPEndPoint(IP_Address, IP_Port);  //将IP地址和端口号绑定到网络节点endpoint上          
            SocketofServer.Bind(Endpoint);  //将负责监听的套接字绑定网络节点        
            SocketofServer.Listen(20);  //开始监听，将套接字的监听队列长度设置为20           
            ThreadofServer = new Thread(WatchConnecting);  //创建一个负责监听客户端的线程        
            ThreadofServer.IsBackground = true;   //将窗体线程设置为与后台同步      
            ThreadofServer.Start();   //启动线程          
            MessageBox.Show("服务器启动监听成功");
        }

        public IPAddress GetLocalIPv4Address()  //获取本地IPv4地址
        {
            IPAddress LocalIPv4 = null;
            IPAddress[] IPAddressList = Dns.GetHostAddresses(Dns.GetHostName());  //获取本机所有的IP地址列表
            foreach (IPAddress IPAddress in IPAddressList)
            {
                if (IPAddress.AddressFamily == AddressFamily.InterNetwork)  //判断是否是IPv4地址
                {
                    LocalIPv4 = IPAddress;
                }
                else
                    continue;
            }
            return LocalIPv4;
        }

        private void WatchConnecting()  //监听客户端请求的方法
        {
            while (true)  //持续不断的监听新的客户端的连接请求            
            {
                //一旦监听到客户端的请求,就返回一个负责和该客户端通信的套接字 
                Socket Socket_ConnectiontoClient = SocketofServer.Accept();
                Client_IP = (SocketofServer.RemoteEndPoint as IPEndPoint).Address; //获取访问客户端的IP  
                Client_Name = "IP:" + Client_IP;  //Client_Name作为客户端的唯一标识
                //将客户端唯一标识和对应的Socket套接字添加到DictSocket                                                          
                DictSocket.Add(Client_Name, Socket_ConnectiontoClient);
                ParameterizedThreadStart pts = new ParameterizedThreadStart(ServerRecMsg);   //创建通信线程
                Thread thread = new Thread(pts);
                thread.IsBackground = true;
                //启动线程,并为线程要调用的方法ServerRecMsg传入参数Socket_ConnectiontoClient
                thread.Start(Socket_ConnectiontoClient);
                //Socket_ConnectiontoClient中保存的是当前连接客户端的Ip和端口   
                MessageBox.Show(Socket_ConnectiontoClient.RemoteEndPoint.ToString() + "客户端连接成功！");
            }
        }

        private void ServerRecMsg(object socketClientPara)  //服务端负责监听客户端发送来的数据的方法
        {
            Socket socketServer = socketClientPara as Socket;  //socketClientPara转换为Socket类型不成功,则返回null
            while (true)
            {
                int length = 0;
                byte[] arrMsgRec = new byte[8 * 1024 * 1024];
                try
                {
                    if (socketServer != null)  //将接收到的数据存入arrMsgRec数组,并返回真正接收到的数据的长度
                        length = socketServer.Receive(arrMsgRec);
                    if (length > 0)  //接受到的长度大于0，说明有信息或文件传来
                    {
                        Client_IP = (socketServer.RemoteEndPoint as IPEndPoint).Address; //获取访问客户端的IP      
                        Client_Port = (socketServer.RemoteEndPoint as IPEndPoint).Port;   //获取访问客户端的Port

                        /*将指定字节数组中的一个字节序列解码为一个字符串
                           public virtual string GetString(
                           byte[] bytes,
                           int index,
                           int count
                                   )
                           参数：
                           bytes 包含要解码的字节序列的字节数组。
                           index 第一个要解码的字节的索引。
                           count 要解码的字节数。
                         */
                        Server_Rec = Encoding.UTF8.GetString(arrMsgRec, 0, length - 1);  //按照UTF8的编码方式得到字符串  

                        if (Client_IP.ToString() == "172.31.8.142")
                        {
                            //处理函数;                         
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("连接异常");
                    break;
                }
            }
        }

        private void ServerSendMsg(string sendMsg) //服务器端发送消息到客户端
        {
            if (sendMsg.Substring(2, 2) == "81")
            {
                byte[] arrSendMsg = Encoding.UTF8.GetBytes(sendMsg);   //将输入的字符串转换成机器可以识别的字节数组             
                DictSocket["IP:192.168.1.201"].Send(arrSendMsg);   //向192.168.1.201发送消息
            }
            else if (sendMsg.Substring(2, 2) == "71")
            {
                byte[] arrSendMsg = Encoding.UTF8.GetBytes(sendMsg);   //将输入的字符串转换成机器可以识别的字节数组                
                DictSocket["IP:192.168.1.202"].Send(arrSendMsg);   //向192.168.1.201发送消息 
            }
        }
        #endregion

        #region 与CTC客户端程序
        public string Client_Rec = "";  //作为客户端接收到的信息
        Socket socketClient = null;
        Thread threadClient = null;  //分别创建1个客户端套接字和1个负责监听服务端请求的线程

        private void CTC系统连接ToolStripMenuItem_Click(object sender, EventArgs e) //作为客户端连接服务器端
        {
            CheckForIllegalCrossThreadCalls = false;
            //定义一个套接字监听,包含3个参数(IP4寻址协议,流式连接,TCP协议)
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //IPAddress serverIPAddress = IPAddress.Parse("192.168.1.217");  //获取服务端IP
            IPAddress serverIPAddress = IPAddress.Parse("192.168.1.216");   //获取服务端IP北京南车务终端
            int serverPort = 21;   //获取服务端Port
            try
            {
                IPEndPoint endpoint = new IPEndPoint(serverIPAddress, serverPort);
                socketClient.Connect(endpoint);  //向指定的ip和端口号的服务端发送连接请求       
                threadClient = new Thread(Client_RecMsg);   //创建一个新线程,用于监听服务端发来的信息            
                threadClient.IsBackground = true;    //将窗体线程设置为与后台同步      
                threadClient.Start();    //启动线程
                flag = true;
                EB_CTC.显示状态 = Electric_Button.Xianshi.绿;
            }
            catch
            {
                MessageBox.Show("连接失败");
            }
        }

        private void Client_RecMsg()  //接收服务器发来信息的方法
        {
            while (true) //持续监听服务端发来的消息
            {
                try
                {
                    //定义一个1M字节的内存缓冲区，用于临时性存储接收到的信息
                    byte[] arrRecMsg = new byte[8 * 1024 * 1024];

                    //将客户端套接字接收到的数据存入内存缓冲区,并获取其长度            
                    int length = socketClient.Receive(arrRecMsg);

                    //将套接字获取到的字节数组转换为人可以看懂的字符串    
                    Client_Rec = Encoding.UTF8.GetString(arrRecMsg, 0, length);

                    //自律模式下办理进路
                    if (Client_Rec.Substring(0, 4) == "AB8F" && Client_Rec.Length == 36)
                    {
                        if (CBI_state.CTC_control_mode)
                        {
                            RouteHandle_fromCTC();
                            Array.Clear(arrRecMsg, 0, arrRecMsg.Length);
                        }
                    }
                    //接收CTC中心允许非常站控模式转换为自律模式
                    else if (Client_Rec.Substring(0, 4) == "ABD8" && Client_Rec.Length == 14)
                    {
                        if (Client_Rec.Substring(8, 4) == "0203")
                        {
                            if (CBI_state.Self_control_mode == true)
                            {
                                CBI_state.CTC_control_mode = true;
                                CBI_state.Self_control_mode = false;
                                CBI_state.Self_to_CTC_mode = true;
                            }
                        }
                    }
                    else
                        MessageBox.Show("信息有误");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    socketClient.Shutdown(SocketShutdown.Both);
                    socketClient.Close();
                    break;
                }
            }          
        }

        private void ClientSendMsg(string sendMsg) //发送字符串信息到服务器的方法
        {
            try
            {
                //将输入的内容字符串转换为机器可以识别的字节数组
               byte[] arrClientSendMsg = Encoding.UTF8.GetBytes(sendMsg);
               //调用客户端套接字发送字节数组
               socketClient.Send(arrClientSendMsg);
            }
            catch(SocketException ex)
            { 
               
                flag = false;
                EB_CTC.显示状态 = Electric_Button.Xianshi.红;
                MessageBox.Show("发送异常：" + ex.Message);
            }
            catch(Exception ex)
            {
               
                flag = false;
                EB_CTC.显示状态 = Electric_Button.Xianshi.红;
                MessageBox.Show("发送异常：" + ex.Message);
            }
           
        }
        #endregion

        #region 与RBC客户端程序
        Socket RBCsocketSend = null;
        Socket RBCsocketSendRoute = null;
        string RBCMessage = "";//作为客户端接收到的信息
        string RBCMessageRoute = "";
        private void rBC系统连接ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            RBCsocketSend = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
            RBCsocketSendRoute = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress RBCserverIPAddress = IPAddress.Parse("192.168.1.206");   //获取RBC服务端IP
            int RBCPort = 10000;//南京南端口号
            int RBCPortRoute = 10001;
            try
            {

                IPEndPoint point = new IPEndPoint(RBCserverIPAddress, RBCPort);
                IPEndPoint pointroute = new IPEndPoint(RBCserverIPAddress, RBCPortRoute);
                RBCsocketSend.Connect(point);  //向指定的ip和端口号的服务端发送连接请求
                RBCsocketSendRoute.Connect(pointroute);
                MessageBox.Show("与RBC连接成功");
                Thread th = new Thread(RBCSevertoClient);//开启一个新的线程用于接收RBC发来的信息
                Thread throute = new Thread(RBCSevertoClientRoute);
                th.IsBackground = true;
                th.Start();
                throute.IsBackground = true;
                throute.Start();
                flagRBC = true;
                EB_RBC.显示状态 = Electric_Button.Xianshi.绿;
            }
            catch
            {
                MessageBox.Show("连接失败");
            }
        }
        /// <summary>
        /// 接收来自RBC的信息
        /// </summary>
        void RBCSevertoClient()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024 * 1024 * 8];
                    int r = RBCsocketSend.Receive(buffer);//receive以字节的形式进行接收
                    if (r == 0)//判断服务器端是否仍在发送
                    {
                        break;
                    }
                    RBCMessage = Encoding.UTF8.GetString(buffer, 0, r);
                }
                catch
                {

                }
            }
        }

        void RBCSevertoClientRoute()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024 * 1024 * 8];
                    int r = RBCsocketSendRoute.Receive(buffer);//receive以字节的形式进行接收
                    if (r == 0)//判断服务器端是否仍在发送
                    {
                        break;
                    }
                    RBCMessageRoute = Encoding.UTF8.GetString(buffer, 0, r);
                }
                catch
                {

                }
            }
        }
        /// <summary>
        /// 联锁向RBC发送信息
        /// </summary>
        /// <param name="Sendmsg"></param>
        private void CBItoRBCmsg(string Sendmsg)//联锁向RBC发送信息
        {
            try
            {
               byte[] buffer = Encoding.UTF8.GetBytes(Sendmsg);
               RBCsocketSend.Send(buffer);
            }
            catch(SocketException ex)
            {
                EB_RBC.显示状态=Electric_Button.Xianshi.红;
                flagRBC=false;
                MessageBox.Show("发送异常："+ex.Message);
            }
           catch(Exception ex)
            {
                EB_RBC.显示状态 = Electric_Button.Xianshi.红;
                flagRBC = false;
               MessageBox.Show("发送异常："+ex.Message);
           }
        }
        public void CBItoRBCRoutemsg(string Sendmsg)//联锁向RBC发送信息
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(Sendmsg);
                RBCsocketSendRoute.Send(buffer);
            }
            catch (SocketException ex)
            {
                EB_RBC.显示状态 = Electric_Button.Xianshi.红;
                flagRBC = false;
                MessageBox.Show("发送异常：" + ex.Message);
            }
            catch (Exception ex)
            {
                EB_RBC.显示状态 = Electric_Button.Xianshi.红;
                flagRBC = false;
                MessageBox.Show("发送异常：" + ex.Message);
            }
        }

        #endregion
        #endregion

        #region 初始化函数
        private void CBI_Nanjing_Load(object sender, EventArgs e)
        {
            asc.controllInitializeSize(this);   //自适应屏幕分辨率

            Station_Init();    //站场初始化
            Section_Init();      //区间初始化
            InterlockingTable_Init();      //联锁表初始化
            Station_Display_Init();      //显示初始化         
          
            ControlsNumber_Init();   //控件编号初始化
            AJ_Init();//按钮继电器初始化
            SingalandTrack_Init();//信号机与接近区段的初始化
            FXJ_Init();//方向继电器初始化
            SwitchState_Init();//道岔状态初始化
            myTopMost = new PswForm(this); 
            Display_Collect();     //采集显示初始化 
            Dianniu_THREAD();  //电钮线程初始化  
        }
      
        private void Station_Init()
        {
            ht_Signal.Clear();
            ht_Track.Clear();  
            ht_SingleActingSwitch.Clear();

            foreach (Control ct in this.Controls)
            {
                //信号机初始化
                if (ct.Name.Contains("NJN"))
                {
                    ht_Signal.Add(ct.Name, ct);
                }
                //股道初始化
                else if (ct.Name.Contains("Track")||ct.Name .Contains("dangui"))
                {
                    Track dg = (Track)ct;                              
                    ht_Track.Add(dg.Name, dg);            
                }       
                //1类道岔（单动道岔）初始化    
                else if (ct.Name.Contains("daocha_1"))
                {
                    Single_Switch dc1 = (Single_Switch)ct;
                    string[] str = dc1.Name.Split('_');
                    dc1.ID号 = str[2];                  
                    ht_SingleActingSwitch.Add(dc1.Name, dc1);
                }            
            }
        }
        private void SwitchState_Init()
        {
            ht_SwitchState.Clear();
            string key = "daocha_1_1";
            int value = 1;
            ht_SwitchState.Add(key,value);

            key = "daocha_1_2";
            value = 1;
            ht_SwitchState.Add(key, value);

            key = "daocha_1_3";
            value = 1;
            ht_SwitchState.Add(key, value);

            key = "daocha_1_4";
            value = 1;
            ht_SwitchState.Add(key, value);

        }
        private void Section_Init()
        {                           
            list_Section.Clear();   //区间信息清零

            //从南京南站场图所有区段中选出区间部分的轨道，初始化，放入list_qjxx
            for (int j = 8; j < 20; j++)
            {
                foreach (Control ct in this.Controls)
                {
                    if (ct.Name.Contains(Drive_Collect.Nanjing_name_CH365_Track[j]))
                    {
                        Track gd = (Track)ct;
                        list_Section.Add(gd);
                    }
                }
            }
        }

        /// <summary>
        /// 联锁表初始化,按照value协议：信号机#道岔%道岔#轨道%轨道添加入哈希表
        /// </summary>
        private void InterlockingTable_Init()
        {          
            ht_InterlockingTable.Clear();
            string key;
            string value;

            #region 列车进路联锁表
            //北京南方向
            //进路1：3G侧线接车（至3G）
            key = "btn_XJ+btn_S3C";
            value = "NJN_X|UU#daocha_1_1|F|S#Track_G3|S";
            ht_InterlockingTable.Add(key, value);
            //3G侧线接车进路取消
           // key = "btn_XRouteCancle+btn_XJ+UU";
            //value = "NJN_X|H#daocha_1_1|F|K#Track_G3|K";
           // ht_InterlockingTable.Add(key, value);

            //进路2: 1G正线接车（至1G）
            key = "btn_XJ+btn_S1C";
            value = "NJN_X|U#daocha_1_1|D|S#Track_G1|S";
            ht_InterlockingTable.Add(key, value);
            //1G正线接车进路取消
           // key = "btn_XRouteCancle+btn_XJ+U";
          //  value = "NJN_X|H#daocha_1_1|D|K#Track_G1|K";
           // ht_InterlockingTable.Add(key, value);

            //进路3：3G侧线发车（由3G）
            key = "btn_S3C+btn_XJ";
            value = "NJN_S3|L#daocha_1_1|F|S#Track_G3|K";
            ht_InterlockingTable.Add(key, value);
            //3G侧线发车取消
            //key = "btn_XRouteCancle+btn_S3C";
           // value = "NJN_S3|H#daocha_1_1|F|K#Track_G3|K";
           // ht_InterlockingTable.Add(key, value);

            //进路4：1G正线发车（由1G）
            key = "btn_S1C+btn_XJ";
            value = "NJN_S1|L#daocha_1_1|D|S#Track_G1|K";
            ht_InterlockingTable.Add(key, value);
            //取消
            //key = "btn_XRouteCancle+btn_S1C";
           // value = "NJN_S1|H#daocha_1_1|D|K#Track_G1|K";
           // ht_InterlockingTable.Add(key, value);

            //进路5：2G正线接车（至2G）
            key = "btn_XFJ+btn_S2C";
            value = "NJN_XF|U#daocha_1_3|D|S#Track_G2|S";
            ht_InterlockingTable.Add(key, value);
            //取消
          //  key = "btn_XRouteCancle+btn_XFJ+U";
           // value = "NJN_XF|H#daocha_1_3|D|K#Track_G2|K";
           // ht_InterlockingTable.Add(key, value);

            //进路6：4G侧线接车（至4G）
            key = "btn_XFJ+btn_S4C";
            value = "NJN_XF|UU#daocha_1_3|F|S#Track_G4|S";
            ht_InterlockingTable.Add(key, value);
            //取消
           // key = "btn_XRouteCancle+btn_XFJ+UU";
           // value = "NJN_XF|H#daocha_1_3|F|K#Track_G4|K";
           // ht_InterlockingTable.Add(key, value);

            //进路7：2G正线发车（由2G）
            key = "btn_S2C+btn_XFJ";
            value = "NJN_S2|L#daocha_1_3|D|S#Track_G2|K";
            ht_InterlockingTable.Add(key, value);
            //取消
           // key = "btn_XRouteCancle+btn_S2C";
           // value = "NJN_S2|H#daocha_1_3|D|K#Track_G2|K";
           // ht_InterlockingTable.Add(key, value);

            //进路8：4G侧线发车（由4G）
            key = "btn_S4C+btn_XFJ";
            value = "NJN_S4|L#daocha_1_3|F|S#Track_G4|S";
            ht_InterlockingTable.Add(key, value);
            //取消
           // key = "btn_XRouteCancle+btn_S4C";
           // value = "NJN_S4|H#daocha_1_3|F|K#Track_G4|K";
            //ht_InterlockingTable.Add(key, value);

            //上海虹桥方向
            //进路9：3G侧线接车（至3G）
            key = "btn_SFJ+btn_X3C";
            value = "NJN_SF|UU#daocha_1_2|F|S#Track_G3|S";
            ht_InterlockingTable.Add(key, value);
            //取消
          //  key = "btn_SRouteCancle+btn_SFJ+UU";
           // value = "NJN_SF|H#daocha_1_2|F|K#Track_G3|K";
           // ht_InterlockingTable.Add(key, value);

            //进路10: 1G正线接车（至1G）
            key = "btn_SFJ+btn_X1C";
            value = "NJN_SF|U#daocha_1_2|D|S#Track_G1|S";
            ht_InterlockingTable.Add(key, value);
            //取消
          //  key = "btn_SRouteCancle+btn_SFJ+U";
           // value = "NJN_SF|H#daocha_1_2|D|K#Track_G1|K";
          //  ht_InterlockingTable.Add(key, value);

            //进路11：3G侧线发车（由3G）
            key = "btn_X3C+btn_SFJ";
            value = "NJN_X3|L#daocha_1_2|F|S#Track_G3|K";
            ht_InterlockingTable.Add(key, value);
            //取消
          //  key = "btn_SRouteCancle+btn_X3C";
          //  value = "NJN_X3|H#daocha_1_2|F|K#Track_G3|K";
           // ht_InterlockingTable.Add(key, value);

            //进路12：1G正线发车（由1G）
            key = "btn_X1C+btn_SFJ";
            value = "NJN_X1|L#daocha_1_2|D|S#Track_G1|K";
            ht_InterlockingTable.Add(key, value);
            //取消
           // key = "btn_SRouteCancle+btn_X1C";
           // value = "NJN_X1|H#daocha_1_2|D|K#Track_G1|K";
            //ht_InterlockingTable.Add(key, value);

            //进路13：2G正线接车（至2G）
            key = "btn_SJ+btn_X2C";
            value = "NJN_S|U#daocha_1_4|D|S#Track_G2|S";
            ht_InterlockingTable.Add(key, value);
            //取消
          //  key = "btn_SRouteCancle+btn_SJ+U";
           // value = "NJN_S|H#daocha_1_4|D|K#Track_G2|K";
           // ht_InterlockingTable.Add(key, value);

            //进路14：4G侧线接车（至4G）
            key = "btn_SJ+btn_X4C";
            value = "NJN_S|UU#daocha_1_4|F|S#Track_G4|S";
            ht_InterlockingTable.Add(key, value);
            //取消
           // key = "btn_SRouteCancle+btn_SJ+UU";
           // value = "NJN_S|H#daocha_1_4|F|K#Track_G4|K";
           // ht_InterlockingTable.Add(key, value);

            //进路15：2G正线发车（由2G）
            key = "btn_X2C+btn_SJ";
            value = "NJN_X2|L#daocha_1_4|D|S#Track_G2|K";
            ht_InterlockingTable.Add(key, value);
            //取消
          //  key = "btn_SRouteCancle+btn_X2C";
           // value = "NJN_X2|H#daocha_1_4|D|K#Track_G2|K";
           // ht_InterlockingTable.Add(key, value);

            //进路16：4G侧线发车（由4G）
            key = "btn_X4C+btn_SJ";
            value = "NJN_X4|L#daocha_1_4|F|S#Track_G4|K";
            ht_InterlockingTable.Add(key, value);
               //取消
          //  key = "btn_SRouteCancle+btn_X4C";
           // value = "NJN_X4|H#daocha_1_4|F|K#Track_G4|K";
           // ht_InterlockingTable.Add(key, value);

            //进路16：下行1G通过
            key = "btn_XT+btn_SFJ";
            value = "NJN_X|L%NJN_X1|L#daocha_1_1|D|S%daocha_1_2|D|S#Track_G1|S";
            ht_InterlockingTable.Add(key, value);

            //进路17：上行1G通过
            key = "btn_SFT+btn_XJ";
            value = "NJN_SF|L%NJN_S1|L#daocha_1_2|D|S%daocha_1_1|D|S#Track_G1|S";
            ht_InterlockingTable.Add(key, value);

            //进路18：上行2G通过
            key = "btn_ST+btn_XFJ";
            value = "NJN_S|L%NJN_S2|L#daocha_1_4|D|S%daocha_1_3|D|S#Track_G2|S";
            ht_InterlockingTable.Add(key, value);
         //进路19：下行2G通过
            key = "btn_XFT+btn_SJ";
            value = "NJN_XF|L%NJN_X2|L#daocha_1_3|D|S%daocha_1_4|D|S#Track_G2|S";
            ht_InterlockingTable.Add(key, value);
            #endregion
        }
        private void SingalandTrack_Init()
        {
            ht_SingalandTrack.Clear();
            String key;
            String value;
            key = "NJN_X";
            value = "dangui_5";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_XF";
            value = "dangui_6";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_S3";
            value = "Track_G3";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_S1";
            value = "Track_G1";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_S2";
            value = "Track_G2";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_S4";
            value = "Track_G4";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_X3";
            value = "Track_G3";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_X1";
            value = "Track_G1";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_X2";
            value = "Track_G2";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_X4";
            value = "Track_G4";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_SF";
            value = "dangui_7";
            ht_SingalandTrack.Add(key, value);

            key = "NJN_S";
            value = "dangui_8";
            ht_SingalandTrack.Add(key, value);

        }
        private void AJ_Init()
        {
            ht_AJ.Clear();//加载时重置按钮继电器哈希表
            string key;
            bool value;
            key = "btn_XJ";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_XFJ";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_S1C";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_S2C";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_S3C";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_S4C";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_X1C";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_X2C";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_X3C";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_X4C";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_SJ";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_SFJ";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_XRouteCancle";
            value = false;
            ht_AJ.Add(key,value);
            key = "btn_SRouteCancle";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_XT";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_XFT";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_ST";
            value = false;
            ht_AJ.Add(key, value);
            key = "btn_SFT";
            value = false;
            ht_AJ.Add(key, value);
        }
        private void FXJ_Init()
        {
            ht_FXJ.Clear();//加载时重置按钮继电器哈希表
            string key;
            int  value;
            key = "btn_XJ";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_XFJ";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_S1C";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_S2C";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_S3C";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_S4C";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_X1C";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_X2C";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_X3C";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_X4C";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_SJ";
            value = 0;
            ht_FXJ.Add(key, value);
            key = "btn_SFJ";
            value = 0;
            ht_FXJ.Add(key, value);
     
        }
        private void Station_Display_Init()
        {
            int flag_xhj = 2;
            for (int j = 0; j < 12; j++)
            {
                if (Drive_Collect.Nanjing_name_CH365_Signal[j] != "")
                {
                    flag_xhj = 0;
                    Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                }
            }
        }
       
        private void Display_Collect()
        {
            Open_Ch365_IO();
            Thread_Collect = new Thread(new ThreadStart(Station_Collect));
            Thread_Collect.IsBackground = true;
            list_Thread.Add(Thread_Collect);
            Thread_Collect.Start();
        }
        /// <summary>
        /// 站场信息初始化
        /// </summary>
        private void Station_Collect()
        {
            while (true)
            {
                Read_Ch365_IO();
                Write_Ch365_IO();
                Show_Station();
                if (flag == true)//自律模式下发送站场信息至CTC
                {
                    ClientSendMsg(Collect_Info());
                }
                if(flagRBC==true )
                {
                    CBItoRBCmsg(Collect_Info());
                }

                Thread.Sleep(250);
            }
        }
     
        private void Dianniu_THREAD()
        {
            Thread_Dianniu_Display = new Thread(new ThreadStart(Dianniu_Display));
            Thread_Dianniu_Display.IsBackground = true;
            list_Thread.Add(Thread_Dianniu_Display);
            Thread_Dianniu_Display.Start();
        }
        private void Dianniu_Display()
        {
            while (true)
            {
                if (CBI_state.CTC_control_mode)
                {
                    EB_right.xianshi = Electric_Button.Xianshi.绿;
                    EB_middle.xianshi = Electric_Button.Xianshi.默认;
                }
                else
                {
                    EB_right.xianshi = Electric_Button.Xianshi.默认;
                }
                if (CBI_state.Self_control_mode)
                {
                    EB_left.xianshi = Electric_Button.Xianshi.红;
                    EB_middle.xianshi = Electric_Button.Xianshi.默认;
                }
                else
                {
                    EB_left.xianshi = Electric_Button.Xianshi.默认;
                }
                if ((CBI_state.Self_to_CTC_mode) && (!CBI_state.CTC_control_mode))
                {
                    EB_middle.xianshi = Electric_Button.Xianshi.黄;
                }
                else
                {
                    EB_middle.xianshi = Electric_Button.Xianshi.默认;
                }
                EB_left.Drawpic();
                EB_middle.Drawpic();
                EB_right.Drawpic();

                //道岔状态指示灯
                if(daocha_1_1.定反位 == Single_Switch.DingFan.定位)
                {
                    btn_Daocha1_1.BackColor = Color.Green;
                }
                else
                {
                    btn_Daocha1_1.BackColor = Color.Yellow;
                }
                if (daocha_1_3.定反位 == Single_Switch.DingFan.定位)
                {
                    btn_Daocha1_3.BackColor = Color.Green;
                }
                else
                {
                    btn_Daocha1_3.BackColor = Color.Yellow;
                }
                if (daocha_1_2.定反位 == Single_Switch.DingFan.定位)
                {
                    btn_Daocha1_2.BackColor = Color.Green;
                }
                else
                {
                    btn_Daocha1_2.BackColor = Color.Yellow;
                }
                if (daocha_1_4.定反位 == Single_Switch.DingFan.定位)
                {
                    btn_Daocha1_4.BackColor = Color.Green;
                }
                else
                {
                    btn_Daocha1_4.BackColor = Color.Yellow;
                }

                Thread.Sleep(750);
            }
        }
        #endregion

        #region 站场股道信息显示
        //显示站场信息      
        private void Show_Station()
        {
            for (int j = 0; j < 20; j++)             //20个轨道
            {
                if (Drive_Collect.Nanjing_name_CH365_Track[j] != "")
                {
                    Control sk = GetPbControl(Drive_Collect.Nanjing_name_CH365_Track[j]);
                    if (sk == null)
                    {
                        return;
                    }
                    else
                    {
                        gd_control_show(sk, j);
                    }
                }
            }
            for (int j = 0; j < 4; j++)            //4个道岔
            {
                if (Drive_Collect.Nanjing_name_CH365_Switch[j] != "")
                {
                    Control sk = GetPbControl(Drive_Collect.Nanjing_name_CH365_Switch[j]);
                    if (sk == null)
                    {
                        return;
                    }
                    else
                    {
                        dc_control_show(sk, j);
                    }
                }
            }
        }

        //显示股道占用信息，flag_zt的值： 1-占用，2-锁闭，3-空闲
        delegate void gd_show_ctr(Control sk, int j);  //声明一个delegate的类型，名为 gd_show_ctr()，它与要传递的方法具有相同的参数和返回值类型。
        private void gd_control_show(Control sk, int j)
        {
            if (!sk.InvokeRequired)
            {
                if (sk.Name.Contains("dangui"))
                {
                    if (Drive_Collect.Collect_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Track[j, 0], Drive_Collect.Nanjing_location_CH365_Track[j, 1], Drive_Collect.Nanjing_location_CH365_Track[j, 2]] == 1)
                    {
                        ((Track)sk).flag_zt = 1;
                    }

                    else if (((Track)sk).flag_zt == 2)
                    { }
                    else
                    {
                        ((Track)sk).flag_zt = 3;
                    }
                    ((Track)sk).Drawpic();
                }
                else if (sk.Name.Contains("Track"))     //站内股道
                {
                    if (Drive_Collect.Collect_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Track[j, 0], Drive_Collect.Nanjing_location_CH365_Track[j, 1], Drive_Collect.Nanjing_location_CH365_Track[j, 2]] == 1)
                    {
                        ((Track)sk).flag_zt = 1;
                    }
                    else if (((Track)sk).flag_zt == 2)
                    { }
                    else
                    {
                        ((Track)sk).flag_zt = 3;
                    }
                    ((Track)sk).Drawpic();
                }
                else if (sk.Name.Contains("daocha_1"))
                {
                    if (Drive_Collect.Collect_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Track[j, 0], Drive_Collect.Nanjing_location_CH365_Track[j, 1], Drive_Collect.Nanjing_location_CH365_Track[j, 2]] == 1)
                    {
                        ((Single_Switch)sk).锁闭状态 = Single_Switch.STATE.占用;
                    }
                    else if (((Single_Switch)sk).锁闭状态 == Single_Switch.STATE.锁闭)
                    { }
                    else
                        ((Single_Switch)sk).锁闭状态 = Single_Switch.STATE.空闲;
                }             
            }
            else
            {
                gd_show_ctr dl = new gd_show_ctr(gd_control_show); //创建delegate对象，并"将要传递的函数作为参数传入"。
                sk.BeginInvoke(dl, new object[] { sk, j });  //调用delegate
            }
        }

        //显示道岔占用信息
        delegate void dc_show_ctr(Control sk, int j);
        private void dc_control_show(Control sk, int j)
        {
            if (!sk.InvokeRequired)
            {
                if (sk.Name.Contains("daocha_1"))   
                {
                    if (Drive_Collect.Collect_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Switch[j, 2, 0], Drive_Collect.Nanjing_location_CH365_Switch[j, 2, 1], Drive_Collect.Nanjing_location_CH365_Switch[j, 2, 2]] == 0)
                        ((Single_Switch)sk).定反位 = Single_Switch.DingFan.反位;
                    else
                        ((Single_Switch)sk).定反位 = Single_Switch.DingFan.定位;
                }             
            }
            else
            {
                dc_show_ctr dl = new dc_show_ctr(dc_control_show);
                sk.BeginInvoke(dl, new object[] { sk, j });
            }
        }
        #endregion

        #region 非常站控模式办理进路，进路取消模块
        /// <summary>
        /// 办理进路按钮
        /// X#daocha_2_1_3|D|X%daocha_2_5_7|D|X#IAG%G3
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_RouteClick(object sender, EventArgs e)
        {
            if (((MouseEventArgs)(e)).Button == MouseButtons.Left)
            {
                string str = ((Button)(sender)).Name;
                if (list_button_Route.Contains(str))
                {
                    MessageBox.Show("已选中");
                }
                else
                {
                    if (((Button)(sender)).Enabled == true)
                    {
                        ((Button)(sender)).Enabled = false;
                    }
                    list_button_Route.Add(((Button)(sender)).Name);
                    ht_AJ[((Button)(sender)).Name] = true;//按钮继电器吸起
                    ((Button)(sender)).BackColor = Color.Red;
                }
            }

            string cz = "";
            foreach (string str in list_button_Route)
            {
                if (cz != "")
                {
                    cz = cz + "+" + str;
                }
                else
                {
                    cz = str;
                }
            }
            //cz记录按下按钮的总名称
            
            string firstbutton = cz.Split('+')[0];//取消按钮或者是进路始端按钮
            string secondbutton = "";
            if(cz.Contains("+"))
            {
               secondbutton = cz.Split('+')[1];//进路始端按钮或者是进路终端按钮
            }
            //ZRJ带动ZQJ，在进路取消的过程中仍然是ZQJ起作用
         //   switch (firstbutton)//分上下行咽喉
          //  {
          //      case "btn_XHandUnlock":
          //          firstbutton = "btn_XRouteCancle";
          //          break;
         //       case "btn_SRouteCancle":
         //           firstbutton = "btn_SRouteCancle";
         //           break;
        //    }
           // String jtrack;//记录始端信号机，由此来找到接近区段   
            string Canclebutton = "";//记录进路的终端
            List<string> AJX = new List<string>();//存储除要取消进路始端其余吸起的继电器
            if (CBI_state.Self_control_mode)//非常站控模式下，进路的取消程序
            {
                if (list_button_Route.Count > 1)//双按钮条件下，判断链表的长度
                {
                    if (((firstbutton == "btn_XRouteCancle" )&&(secondbutton=="btn_XJ"||secondbutton=="btn_XFJ"||secondbutton=="btn_S1C"||secondbutton=="btn_S2C"||secondbutton=="btn_S3C"||secondbutton=="btn_S4C"||secondbutton=="btn_XT"||secondbutton=="btn_XFT"))
                        ||((firstbutton == "btn_SRouteCancle" )&&(secondbutton=="btn_SJ"||secondbutton=="btn_SFJ"||secondbutton=="btn_X1C"||secondbutton=="btn_X2C"||secondbutton=="btn_X3C"||secondbutton=="btn_X4C"||secondbutton=="btn_ST"||secondbutton=="btn_SFT"))) //取消按钮被按下
                    {
                        
                        foreach (var item in ht_AJ.Keys)
                        {
                            if ((bool)(ht_AJ[item]) == true && (string)(item) != secondbutton && (string)(item) != "btn_XRouteCancle" && (string)(item) != "btn_SRouteCancle")//
                            {
                                AJX.Add((string)item);//存储除进路始端按钮和取消按钮之外的吸起继电器信息
                            }
                        }
                        foreach (string  st in AJX)
                        {
                            if(ht_InterlockingTable.Contains(secondbutton+"+"+st))//此处存在按下的是终端按钮时同样可以对应到联锁表
                            {
                                Canclebutton = st;
                            }
                        }
                        if (Canclebutton == "")//无终端按钮，无进路
                        {
                            MessageBox.Show("请先办理进路");
                            //按钮恢复
                            foreach (string st in list_button_Route)
                            {
                                foreach (Control ct in Controls)
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
                            list_button_Route.Clear();//把按钮存的信息清理
                            ht_AJ[secondbutton] = false;//恢复按钮继电器的状态

                        }//null的判断
                        else
                        {
                            if ((secondbutton == "btn_XJ" || secondbutton == "btn_XFJ" || secondbutton == "btn_SJ" || secondbutton == "btn_SFJ") && ((int )(ht_FXJ[secondbutton])==1)) //接车取消
                            {
                                ht_AJ[secondbutton] = false;//按钮继电器恢复
                                if (Canclebutton != "")
                                {
                                    ht_AJ[Canclebutton] = false;
                                }
                               
                               
                                secondbutton = secondbutton + "+" + Canclebutton;
                                string info = (string)(ht_InterlockingTable[secondbutton]);//找到对应的联锁表信息
                                string xhj = info.Split('#')[0];
                                string xhj2 = xhj.Split('|')[0];

                                //通过信号机找到接近区段
                                String jtrack = (string)ht_SingalandTrack[xhj2];//根据信号机找到对应的接近区段的名称


                                string dc = info.Split('#')[1];
                                string dg = info.Split('#')[2];
                                {
                                    if (((Track)ht_Track[jtrack]).flag_zt == 3)//1==占用，2==锁闭 3==空闲
                                    {
                                        RouteCancle_Mark(xhj, dc, dg);
                                        foreach (string t in list_InterlockingTable)
                                        {
                                            if (t == info)
                                            {
                                                list_InterlockingTable.Remove(t);    //移除进路信息
                                                break;
                                            }
                                        }

                                        foreach (var item in list_InterlockingTable)
                                        {
                                            CBItoRBCRoutemsg(item);//此处向RBC更新进路的信息
                                        }
                                        //按钮恢复正常
                                        foreach (string item in list_button_Route)
                                        {
                                            foreach (Control ct in Controls)//效率很慢
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
                                        list_button_Route.Clear();
                                        RouteNum--;
                                        ht_FXJ[secondbutton.Split('+')[0]] = 0;

                                    }//判断接近区段的情况
                                   // else if (((Track)ht_Track[jtrack]).flag_zt == 1)
                                   // {
                                   //    fun();
                                   // }
                                }//string下的

                            }//if
                            else if ((secondbutton=="btn_X1C"||secondbutton=="btn_X2C"||secondbutton=="btn_X3C"||secondbutton=="btn_X4C"||secondbutton=="btn_S1C"
                                || secondbutton == "btn_S2C" || secondbutton == "btn_S3C" || secondbutton == "btn_S4C") && ((int)(ht_FXJ[secondbutton])==-1))//发车取消
                            {
                                ht_AJ[secondbutton] = false;//按钮继电器恢复
                                if (Canclebutton != "")
                                {
                                    ht_AJ[Canclebutton] = false;
                                }
                                secondbutton = secondbutton + "+" + Canclebutton;
                                string info = (string)(ht_InterlockingTable[secondbutton]);
                                string xhj = info.Split('#')[0];
                                string dc = info.Split('#')[1];
                                string dg = info.Split('#')[2];
                                {
                                    RouteCancle_Mark(xhj, dc, dg);

                                    foreach (string t in list_InterlockingTable)
                                    {
                                        if (t == info)
                                        {
                                            list_InterlockingTable.Remove(t);    //移除进路信息
                                            break;
                                        }
                                    }

                                    foreach (var item in list_InterlockingTable)
                                    {
                                        CBItoRBCRoutemsg(item);//更新进路信息
                                    }
                                    //按钮恢复正常
                                    foreach (string item in list_button_Route)
                                    {
                                        foreach (Control ct in Controls)//效率很慢
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
                                    list_button_Route.Clear();
                                    RouteNum--;
                                    ht_FXJ[secondbutton.Split('+')[0]] = 0;


                                }//string下的
                            }//else下的
                            else if ((secondbutton == "btn_XT" || secondbutton == "btn_XFT" || secondbutton == "btn_ST" || secondbutton == "btn_SFT" && ((int)(ht_FXJ[secondbutton]) ==2)))
                            {
                                //通过进路的取消
                                ht_AJ[secondbutton] = false;//按钮继电器恢复
                                if (Canclebutton != "")
                                {
                                    ht_AJ[Canclebutton] = false;
                                }
                                secondbutton = secondbutton + "+" + Canclebutton;
                                string info = (string)(ht_InterlockingTable[secondbutton]);
                                string xhj = info.Split('#')[0];
                                string dc = info.Split('#')[1];
                                string dg = info.Split('#')[2];
                                {

                                    RouteCancle_Mark(xhj, dc, dg);
                                    foreach (string t in list_InterlockingTable)
                                    {
                                        if (t == info)
                                        {
                                            list_InterlockingTable.Remove(t);    //移除进路信息
                                            break;
                                        }
                                    }

                                    foreach (var item in list_InterlockingTable)
                                    {
                                        CBItoRBCRoutemsg(item);//更新进路信息
                                    }
                                    //按钮恢复正常
                                    foreach (string item in list_button_Route)
                                    {
                                        foreach (Control ct in Controls)//效率很慢
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
                                    list_button_Route.Clear();
                                    RouteNum--;
                                    ht_FXJ[secondbutton.Split('+')[0]] = 0;
                                }//string下的
                            }
                            else 
                            {
                                MessageBox.Show("进路取消请按取消按钮+进路始端按钮");
                                //按钮恢复
                                foreach (string item in list_button_Route)
                                {
                                    foreach (Control ct in Controls)//效率很慢
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
                                list_button_Route.Clear();
                            }
                        }//else

                   
                    }//进路取消按钮条件进入
                    #region 总人解按钮取消进路
                    //思路：首先根据按钮名称判断是否为总人解按钮，然后根据进路的始端按钮找到联锁表信息，确定具体的进路信息
                    // 在进行进路取消前要进行fun()函数进行倒计时，开一个新线程等待倒计时程序中标志变量的变化，在变量转变后，调用取消进路函数进行取消。
                    //难点涉及：1.线程的开销，要考虑等待的过程中界面上其余的功能要正常的实现
                    //2.要区分出发车进路还是接车进路，以此来选取倒计时的30秒和180秒
                    //3.取消进路函数的封装，public void RouteCancle(String FunctionButton ,String StartButton );
                    // RouteRemove routeremove=new RouteRemove();
                    if (firstbutton == "btn_XHandUnlock" || firstbutton == "btn_SHandUnlock")//此时的程序为进行总人解功能
                    {
                        //new Thread(fun).Start();//新开线程进行倒计时的等待
                             
                              switch (firstbutton)//分上下行咽喉
                             {
                                 case "btn_XHandUnlock":
                                    firstbutton = "btn_XRouteCancle";
                                    break;
                                  case "btn_SRouteCancle":
                                     firstbutton = "btn_SRouteCancle";
                                      break;
                              }
                              
                              //调用封装的取消进路类   可以取消，但是计时的时间不到，没有响应事件
                              routeremove = new RouteRemove(firstbutton, secondbutton);
                              Thread ZongrenThread=new Thread(Counter);//利用线程启动计时触发命令
                              ZongrenThread.IsBackground = true;
                              ZongrenThread.Start();
                    }
                    
                    #endregion
                   


                    //    if (ht_InterlockingTable.Contains(cz))
                    //    {
                    //        //将联锁表信息分离 [0]信号灯#[1]道岔#[2]股道
                    //        string info = (string)(ht_InterlockingTable[cz]);
                    //        string xhj = info.Split('#')[0];
                    //        string dc = info.Split('#')[1];
                    //        string gd = info.Split('#')[2];
                    //        {
                    //            RouteCancle_Mark(xhj, dc, gd);

                    //            //按钮恢复
                    //            int count = 0;
                    //            foreach (string st in list_button_Route)
                    //            {
                    //                foreach (Control ct in Controls)
                    //                {
                    //                    if (ct.Name == st)
                    //                    {
                    //                        ((Button)ct).Enabled = true;
                    //                        ((Button)ct).BackColor = Color.Green;
                    //                    }
                    //                }
                    //                count++;
                    //            }
                    //            list_button_Route.Clear();
                    //        }
                    //    }                    
                    //}             

                    else if (ht_InterlockingTable.Contains(cz))//进路办理程序
                    {
                          
                        if (cz.Split('+')[0] == "btn_XJ" || cz.Split('+')[0] == "btn_XFJ" || cz.Split('+')[0] == "btn_SJ" || cz.Split('+')[0] == "btn_SFJ")
                       {

                           ht_FXJ[cz.Split('+')[0]] = 1;//1代表接车方向
                           
                       }
                        else if (cz.Split('+')[0] == "btn_S3C" || cz.Split('+')[0] == "btn_S1C" || cz.Split('+')[0] == "btn_S2C" || cz.Split('+')[0] == "btn_S4C"
                            || cz.Split('+')[0] == "btn_X3C" || cz.Split('+')[0] == "btn_X1C" || cz.Split('+')[0] == "btn_X2C"||cz.Split('+')[0]=="btn_X4C")
                        {

                            ht_FXJ[cz.Split('+')[0]] = -1;//-1代表发车方向
                        }
                        else
                        {
                            ht_FXJ[cz.Split('+')[0]] = 2;//通过进路
                        }
                        //将联锁表信息分离 [0]信号灯#[1]道岔#[2]股道
                        string info = (string)(ht_InterlockingTable[cz]);
                        string xhj = info.Split('#')[0];
                        string dc = info.Split('#')[1];
                        string gd = info.Split('#')[2];
                        {
                            if (Route_Occupy_Check(info, sender))
                            {
                                if (Route_Function(info, sender))
                                {
                                    Route_Mark(xhj, dc, gd);
                                    RouteNum++;//进路条数自加
                                    //按钮恢复
                                    int count = 0;
                                    foreach (string st in list_button_Route)
                                    {
                                        foreach (Control ct in Controls)
                                        {
                                            if (ct.Name == st)
                                            {
                                                ((Button)ct).Enabled = true;
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
                                        count++;
                                    }
                                    list_button_Route.Clear();
                                }
                                else
                                {
                                    MessageBox.Show("进路无效");
                                    //按钮恢复
                                    int count = 0;
                                    foreach (string st in list_button_Route)
                                    {
                                        foreach (Control ct in Controls)
                                        {
                                            if (ct.Name == st)
                                            {
                                                ((Button)ct).Enabled = true;
                                                ((Button)ct).BackColor = Color.Green;
                                            }
                                        }
                                        count++;
                                    }
                                    list_button_Route.Clear();
                                }
                            }
                            else
                            {
                                MessageBox.Show("进路被占用或处于封锁状态");
                                //按钮恢复
                                int count = 0;
                                foreach (string st in list_button_Route)
                                {
                                    foreach (Control ct in Controls)
                                    {
                                        if (ct.Name == st)
                                        {
                                            ((Button)ct).Enabled = true;
                                            //((Button)ct).BackColor = Color.Green;
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
                                    count++;
                                }
                                list_button_Route.Clear();
                            }
                        }
                    }//进路办理else if
                    else
                    {
                        MessageBox.Show("命令非法！");
                        //按钮恢复
                        int count = 0;
                        foreach (string st in list_button_Route)
                        {
                            foreach (Control ct in Controls)
                            {
                                if (ct.Name == st)
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
                            count++;
                        }
                        list_button_Route.Clear();
                    }
                }         
            }
            else
            {
                MessageBox.Show("当前为自律模式");
            }
        }                              
        #endregion

        #region 进路处理函数，具体进路办理过程：（1）检查列车股道是否有占用 （2）转辙机办理 （3）检查转辙机位置 （4）开放信号

        //检查列车轨道是否有占用
        private bool Route_Occupy_Check(string info, object sender)
        {
            string xhj = info.Split('#')[0];
            string dc = info.Split('#')[1];
            string gd = info.Split('#')[2];

            //轨道部分
            if (gd != "")
            {
                string[] gds = gd.Split('%');
                foreach (string d in gds)
                {
                    string name = d.Split('|')[0];
                    if (((Track)ht_Track[name]).flag_zt == 2 || ((Track)ht_Track[name]).flag_zt == 1)   //白光带，锁闭状态和红光带，占用状态
                    {
                        return false;
                    }
                    else
                    {

                    }
                }
            }
            //道岔部分
            if (dc != "")
            {
                string[] dcs = dc.Split('%');
                Color switchcolor=Color.Red;
                foreach (string d in dcs)
                {
                    if (d.Contains("daocha_1"))
                    {
                        string[] st = d.Split('|');
                        string name = st[0];
                        string df = st[1];

                        if (df == "D")
                        {
                            switchcolor = Color.Green;
                        }
                        else if (df == "F")
                        {
                            switchcolor = Color.Yellow;
                        }
                        string [] name1=name.Split('_');
                        string zt = st[2];
                        if ((((Single_Switch)ht_SingleActingSwitch[name]).锁闭状态 == Single_Switch.STATE.锁闭 && (int)ht_SwitchState[name]==2) || ((Single_Switch)ht_SingleActingSwitch[name]).锁闭状态 == Single_Switch.STATE.占用 || label111.BackColor == Color.Purple || label112.BackColor == Color.Purple || label113.BackColor == Color.Purple || label114.BackColor == Color.Purple)//判断是否锁闭占用或者道岔被封锁
                        {
                            return false; 
                        }
                        else if ((((Single_Switch)ht_SingleActingSwitch[name]).锁闭状态 == Single_Switch.STATE.锁闭 ) && (switchcolor != this.Controls["btn_Daocha1_" + name1[2]].BackColor))//判断锁闭状态下联锁表的定反位与锁闭的定反位是否一致
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        //加入到列车联锁信息列表
        private bool Route_Function(string info, object sender)
        {
            string xhj = info.Split('#')[0];
            string dc = info.Split('#')[1];
            string gd = info.Split('#')[2];

            if (Route_Mark(xhj, dc, gd))
            {
                if (EB_RBC.显示状态==Electric_Button.Xianshi.绿)
                {
                    list_InterlockingTable.Add(info);    //保存进路信息
                }
                //else if (((Button)sender).Name.Contains("Route_RouteCancle"))
                //{
                //    foreach (string t in list_InterlockingTable)
                //    {
                //        if (t.Contains(info.Split('|')[0]))
                //        {
                //            list_InterlockingTable.Remove(t);    //移除进路信息
                //            break;
                //        }
                //    }
                //}
                foreach (var item in list_InterlockingTable)
                {
                    CBItoRBCRoutemsg(item);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        //办理进路标记模块
        private bool Route_Mark(string Route_Signal, string Route_Switch, string Route_Track)
        {
            #region 信号灯显示标记
            if(Route_Signal!="")
            {
                string[] signals = Route_Signal.Split('%');
                 foreach (string st in signals)
                 {
                     if (st.Contains("NJN"))
                     {
                         string xhj_name = st.Split('|')[0];
                         string xhj_state = st.Split('|')[1];
                         switch (xhj_state)
                         {
                             case "UU":
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
                                 break;
                         }//switch
                         ((Train_Signal)(ht_Signal[xhj_name])).drawpic();
                         Route_Signal_Drive(xhj_name, xhj_state);

                     }//if NJN
                 }//foreach
            }//if Signa
                    #region
                    //if (Route_Signal != "")
                    //{

                    //    if (Route_Signal.Contains("NJN"))
                    //    {
                    //        string xhj_name = Route_Signal.Split('|')[0];
                    //        string xhj_state = Route_Signal.Split('|')[1];
                    //        switch (xhj_state)
                    //        {
                    //            case "UU":
                    //                ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 2;
                    //                break;
                    //            case "U":
                    //                ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 1;
                    //                break;
                    //            case "H":
                    //                ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 5;
                    //                break;
                    //            case "B":
                    //                ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 4;
                    //                break;
                    //            case "L":
                    //                ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 3;
                    //                break;
                    //            case "LH":
                    //                ((Train_Signal)(ht_Signal[xhj_name])).X_flag = 6;
                    //                break;
                    //        }
                    //        ((Train_Signal)(ht_Signal[xhj_name])).drawpic();
                    //        Route_Signal_Drive(xhj_name, xhj_state);
                    //    }
                    #endregion
                
            #endregion

            #region 道岔标记
            if (Route_Switch != "")
            {
                string[] switches = Route_Switch.Split('%');
                foreach (string sws in switches)
                {                 
                    if (sws.Contains("daocha_1"))
                    {
                        string[] st = sws.Split('|');
                        string name = st[0];
                        string df = st[1];
                        string zt = st[2];
                        ((Single_Switch)ht_SingleActingSwitch[name]).dancao = Single_Switch.DanCao.进路;
                        switch (df)
                        {
                            case "D":
                                if (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.定位 )
                                {

                                }
                                else if (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.反位) //&& (int)ht_SwitchState[name] != 3) || (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.反位 && (int)ht_SwitchState[name] != 2))//反位非封锁、反位非锁闭可以办理
                                {
                                    ((Single_Switch)ht_SingleActingSwitch[name]).定反位 = Single_Switch.DingFan.定位;
                                }
                               // else if( ((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.反位 && (int)ht_SwitchState[name] == 3)//反位封锁不可办理
                               // {
                               //     MessageBox.Show("进路中有道岔处于单封状态");
                               //     
                               // }
                               // else if (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.反位 && (int)ht_SwitchState[name] == 2)
                               // {
                               //     MessageBox.Show("进路中有道岔处于单锁状态");
                              //     
                              //  }
                                Route_Switch_Drive(name, df);
                                break;
                            case "F":
                                if (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.反位 )
                                {

                                }
                                else if (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.定位)// &&(int)ht_SwitchState[name]!=2) ||(((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.定位 &&(int)ht_SwitchState[name]!=3))//定位未锁闭，定位未封锁
                                {
                                    ((Single_Switch)ht_SingleActingSwitch[name]).定反位 = Single_Switch.DingFan.反位;
                                }
                               // else if(((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.定位 && (int)ht_SwitchState[name] == 3)
                              //  {
                               //     MessageBox.Show("进路中有道岔处于单封状态");
                                   
                              //  }
                               // else if (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.定位 && (int)ht_SwitchState[name] == 2)
                               // {
                               //     MessageBox.Show("进路中有道岔处于单锁状态");
                                   
                               // }
                                Route_Switch_Drive(name, df);
                                break;
                        }
                        switch (zt)
                        {
                            case "Z":
                                ((Single_Switch)ht_SingleActingSwitch[name]).锁闭状态 = Single_Switch.STATE.占用;
                                break;
                            case "S":
                                ((Single_Switch)ht_SingleActingSwitch[name]).锁闭状态 = Single_Switch.STATE.锁闭;
                                break;
                            case "K":
                                ((Single_Switch)ht_SingleActingSwitch[name]).锁闭状态 = Single_Switch.STATE.空闲;
                                break;
                        }
                    }
                }
            }
            #endregion

            #region 轨道显示标记
            if (Route_Track != "")
            {
                string[] gds = Route_Track.Split('%');
                foreach (string d in gds)
                {
                    string name = d.Split('|')[0];
                    string zt = d.Split('|')[1];
                    if (zt == "S")   //锁闭状态
                    {
                        if (((Track)ht_Track[name]).flag_zt == 2)
                        {
                        }
                        else
                        {
                            ((Track)ht_Track[name]).flag_zt = 2;
                            ((Track)ht_Track[name]).Drawpic();
                        }
                    }
                    else if (zt == "Z")   //占用状态
                    {
                        if (((Track)ht_Track[name]).flag_zt == 1)
                        {
                        }
                        else
                        {
                            ((Track)ht_Track[name]).flag_zt = 1;
                            ((Track)ht_Track[name]).Drawpic();
                        }
                    }
                    else if (zt == "K")   //空闲状态
                    {
                        if (((Track)ht_Track[name]).flag_zt == 3)
                        {
                        }
                        else
                        {
                            ((Track)ht_Track[name]).flag_zt = 3;
                            ((Track)ht_Track[name]).Drawpic();
                        }
                    }
                }
            }
            #endregion        

            return true;
        }

        //取消进路标记模块
        public bool RouteCancle_Mark(string Route_Signal, string Route_Switch, string Route_Track)
        {
           
            #region 信号机标记新
            if (Route_Signal != "")
            {
                string[] signals = Route_Signal.Split('%');
                foreach (var item in signals)
                {
                    if(item.Contains("NJN"))
                    {
                        string Signal_Name = item.Split('|')[0];
                        string Signal_Display = item.Split('|')[1];
                        Control sk = GetPbControl(Signal_Name);
                        switch (Signal_Display)
                        {
                            case "UU":
                                ((Train_Signal)(ht_Signal[Signal_Name])).X_flag = 5;
                                break;
                            case "U":
                                ((Train_Signal)(ht_Signal[Signal_Name])).X_flag = 5;
                                break;
                            case "L":
                                ((Train_Signal)(ht_Signal[Signal_Name])).X_flag = 5;
                                break;
                            case "B":
                                ((Train_Signal)(ht_Signal[Signal_Name])).X_flag = 5;
                                break;
                            case "H":
                                ((Train_Signal)(ht_Signal[Signal_Name])).X_flag = 5;
                                break;
                            case "LU":
                                ((Train_Signal)(ht_Signal[Signal_Name])).X_flag = 5;
                                break;
                        }//switch
                        ((Train_Signal)(ht_Signal[Signal_Name])).drawpic();
                         Route_Signal_Drive(Signal_Name, Signal_Display);
                    }//if NJN
                }//forach
             
            }//if signal
            #endregion

            #region 道岔标记
            if (Route_Switch != "")
            {
                string[] switches = Route_Switch.Split('%');
                foreach (string sws in switches)
                {
                    if (sws.Contains("daocha_1"))
                    {
                        string[] st = sws.Split('|');
                        string name = st[0];
                        string df = st[1];
                        string zt = st[2];
                        //switch (df)
                        //{
                        //    case "D":
                        //        if (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.定位)
                        //        {

                        //        }
                        //        else if (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.反位)
                        //        {
                        //            ((Single_Switch)ht_SingleActingSwitch[name]).定反位 = Single_Switch.DingFan.定位;
                        //        }                     
                        //        break;
                        //    case "F":
                        //        if (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.反位)
                        //        {

                        //        }
                        //        else if (((Single_Switch)ht_SingleActingSwitch[name]).定反位 == Single_Switch.DingFan.定位)
                        //        {
                        //            ((Single_Switch)ht_SingleActingSwitch[name]).定反位 = Single_Switch.DingFan.反位;
                        //        }                           
                        //        break;
                        //}
                        switch (zt)
                        {
                            case "Z":
                                ((Single_Switch)ht_SingleActingSwitch[name]).锁闭状态 = Single_Switch.STATE.空闲;
                                break;
                            case "S":
                                ((Single_Switch)ht_SingleActingSwitch[name]).锁闭状态 = Single_Switch.STATE.空闲;
                                break;
                            case "K":
                                ((Single_Switch)ht_SingleActingSwitch[name]).锁闭状态 = Single_Switch.STATE.空闲;
                                break;
                        }
                    }
                }
            }
            #endregion

            #region 轨道显示标记
            if (Route_Track != "")
            {
                string[] gds = Route_Track.Split('%');
                foreach (string d in gds)
                {
                    string name = d.Split('|')[0];
                    string zt = d.Split('|')[1];
                    if (zt == "S")
                    {
                        if (((Track)ht_Track[name]).flag_zt == 3)  //已经标记为空闲状态
                        {
                            ((Track)ht_Track[name]).flag_zt = 3;
                            ((Track)ht_Track[name]).Drawpic();
                        }
                        else
                        {
                            ((Track)ht_Track[name]).flag_zt = 3;
                            ((Track)ht_Track[name]).Drawpic();
                        }
                    }
                    //if (zt == "S")   //锁闭状态
                    //{
                    //    if (((Track)ht_Track[name]).flag_zt == 2)
                    //    {
                    //    }
                    //    else
                    //    {
                    //        ((Track)ht_Track[name]).flag_zt = 2;
                    //        ((Track)ht_Track[name]).Drawpic();
                    //    }
                    //}
                    else if (zt == "Z")   //占用状态
                    {
                        if (((Track)ht_Track[name]).flag_zt == 3)
                        {
                        }
                        else
                        {
                            ((Track)ht_Track[name]).flag_zt = 3;
                            ((Track)ht_Track[name]).Drawpic();
                        }
                    }
                    else if (zt == "K")   //空闲状态
                    {
                        if (((Track)ht_Track[name]).flag_zt == 3)
                        {
                        }
                        else
                        {
                            ((Track)ht_Track[name]).flag_zt = 3;
                            ((Track)ht_Track[name]).Drawpic();
                        }
                    }
                }
            }
            #endregion        

            return true;
        }

        //进路信号机驱动显示模块
        private void Route_Signal_Drive(string Signal_Name, string Signal_State)
        {
            int flag_xhj = 0;
            for (int j = 0; j < 12; j++)
            {
                if (Drive_Collect.Nanjing_name_CH365_Signal[j] == Signal_Name)
                {
                    switch (Signal_State)
                    {
                        case "UU":
                            flag_xhj = 0;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                            flag_xhj = 1;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 2;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 3;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                            flag_xhj = 4;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            break;
                        case "U":
                            flag_xhj = 0;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                            flag_xhj = 1;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 2;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 3;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 4;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            break;
                        case "H":
                            flag_xhj = 0;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 1;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 2;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                            flag_xhj = 3;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 4;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            break;
                        case "B":
                            flag_xhj = 0;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 1;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 2;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 3;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 4;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                            break;
                        case "L":
                            flag_xhj = 0;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 1;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                            flag_xhj = 2;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 3;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 4;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            break;
                        case "UL":
                            flag_xhj = 0;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                            flag_xhj = 1;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                            flag_xhj = 2;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 3;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 4;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            break;
                        case "HB":
                            flag_xhj = 0;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 1;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 2;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                            flag_xhj = 3;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 0;
                            flag_xhj = 4;
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 0], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 1], Drive_Collect.Nanjing_location_CH365_Signal[j, flag_xhj, 2]] = 1;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        //进路道岔(转辙机)驱动模块
        private void Route_Switch_Drive(string Switch_Name, string Switch_State)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Drive_Collect.Nanjing_name_CH365_Switch[j] == Switch_Name)
                {
                    switch (Switch_State)
                    {
                        case "D":
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Switch[j, 0, 0], Drive_Collect.Nanjing_location_CH365_Switch[j, 0, 1], Drive_Collect.Nanjing_location_CH365_Switch[j, 0, 2]] = 1;
                            break;
                        case "F":
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Switch[j, 1, 0], Drive_Collect.Nanjing_location_CH365_Switch[j, 1, 1], Drive_Collect.Nanjing_location_CH365_Switch[j, 1, 2]] = 1;
                            break;
                        default:
                            break;
                    }
                }
            }
            Thread.Sleep(300);
            for (int j = 0; j < 4; j++)
            {
                if (Drive_Collect.Nanjing_name_CH365_Switch[j] == Switch_Name)
                {
                    switch (Switch_State)
                    {
                        case "D":
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Switch[j, 0, 0], Drive_Collect.Nanjing_location_CH365_Switch[j, 0, 1], Drive_Collect.Nanjing_location_CH365_Switch[j, 0, 2]] = 0;
                            break;
                        case "F":
                            Drive_Collect.Drive_Split_Data_CH365_IO[Drive_Collect.Nanjing_location_CH365_Switch[j, 1, 0], Drive_Collect.Nanjing_location_CH365_Switch[j, 1, 1], Drive_Collect.Nanjing_location_CH365_Switch[j, 1, 2]] = 0;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        #endregion

        #region menustrip菜单处理函数       
        private void 申请自律模式ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CBI_state.CTC_control_mode = true;
            CBI_state.Self_control_mode = false;
            CBI_state.Self_to_CTC_mode = false;
            //string Apply_Autonomous = "ABD811020103AC";
            //ClientSendMsg(Apply_Autonomous);
        }

        private void 退出自律模式ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CBI_state.CTC_control_mode = false;
            CBI_state.Self_control_mode = true;
            CBI_state.Self_to_CTC_mode = true;
            //string SeltControl = "ABD811020404AC";
            //ClientSendMsg(SeltControl);
        }
        #endregion

        #region 将采集到的站场信息形成传递给CTC与RBC的通信字符串
        public string Collect_Info()
        {
            string Collect_Info = "";          

            //判断南京南IIIG股道的状态
            string a="";
            if (Track_G3.flag_zt == 3)
            {
                a = "01";
            }
            else if (Track_G3.flag_zt == 2)
            {
                a = "02";
            }
            else if (Track_G3.flag_zt == 1)
            {
                a = "03";
            }

            //判断南京南IG股道的状态
            string b="";
            if (Track_G1.flag_zt == 3)
            {
                b = "01";
            }
            if (Track_G1.flag_zt == 2)
            {
                b = "02";
            }
            if (Track_G1.flag_zt == 1)
            {
                b = "03";
            }

            //判断南京南IIG股道的状态
            string c="";
            if (Track_G2.flag_zt == 3)
            {
                c = "01";
            }
            if (Track_G2.flag_zt == 2)
            {
                c = "02";
            }
            if (Track_G2.flag_zt == 1)
            {
                c = "03";
            }

            //判断南京南4G股道的状态
            string d="";
            if (Track_G4.flag_zt == 3)
            {
                d = "01";
            }
            if (Track_G4.flag_zt == 2)
            {
                d = "02";
            }
            if (Track_G4.flag_zt == 1)
            {
                d = "03";
            }

            //判断南京南X1JG的状态
            string e;
            if (dangui_1.flag_zt == 3)
            {
                e = "01";
            }
            else if (dangui_1.flag_zt == 2)
            {
                e = "02";
            }
            else
            {
                e = "03";
            }

            //判断南京南X2JG的状态
            string f;
            if (dangui_3.flag_zt == 3)
            {
                f = "01";
            }
            else if (dangui_3.flag_zt == 2)
            {
                f = "02";
            }
            else
            {
                f = "03";
            }

            //判断南京南X3JG的状态
            string g;
            if (dangui_5.flag_zt == 3)
            {
                g = "01";
            }
            else if (dangui_5.flag_zt == 2)
            {
                g = "02";
            }
            else
            {
                g = "03";
            }

            //判断南京南X1LQ的状态
            string h;
            if (dangui_7.flag_zt == 3)
            {
                h = "01";
            }
            else if (dangui_7.flag_zt == 2)
            {
                h = "02";
            }
            else
            {
                h = "03";
            }

            //判断南京南X2LQ的状态
            string i;
            if (dangui_9.flag_zt == 3)
            {
                i = "01";
            }
            else if (dangui_9.flag_zt == 2)
            {
                i = "02";
            }
            else
            {
                i = "03";
            }

            //判断南京南X3LQ的状态
            string j;
            if (dangui_11.flag_zt == 3)
            {
                j = "01";
            }
            else if (dangui_11.flag_zt == 2)
            {
                j = "02";
            }
            else
            {
                j = "03";
            }

            //判断南京南S3LQ的状态
            string k;
            if (dangui_2.flag_zt == 3)
            {
                k = "01";
            }
            else if (dangui_2.flag_zt == 2)
            {
                k = "02";
            }
            else
            {
                k = "03";
            }

            //判断南京南S2LQ的状态
            string l;
            if (dangui_4.flag_zt == 3)
            {
                l = "01";
            }
            else if (dangui_4.flag_zt == 2)
            {
                l = "02";
            }
            else
            {
                l = "03";
            }

            //判断南京南S1LQ的状态
            string m;
            if (dangui_6.flag_zt == 3)
            {
                m = "01";
            }
            else if (dangui_6.flag_zt == 2)
            {
                m = "02";
            }
            else
            {
                m = "03";
            }

            //判断南京南S3JG的状态
            string n;
            if (dangui_8.flag_zt == 3)
            {
                n = "01";
            }
            else if (dangui_8.flag_zt == 2)
            {
                n = "02";
            }
            else
            {
                n = "03";
            }

            //判断南京南S2JG的状态
            string o;
            if (dangui_10.flag_zt == 3)
            {
                o = "01";
            }
            else if (dangui_10.flag_zt == 2)
            {
                o = "02";
            }
            else
            {
                o = "03";
            }

            //判断南京南S1JG的状态
            string p;
            if (dangui_12.flag_zt == 3)
            {
                p = "01";
            }
            else if (dangui_12.flag_zt == 2)
            {
                p = "02";
            }
            else
            {
                p = "03";
            }

            //判断南京南DC1道轨的状态
            string q;
            if (daocha_1_1.定反位 == Single_Switch.DingFan.定位 && daocha_1_1.锁闭状态 == Single_Switch.STATE.空闲)
            {
                q = "01";
            }
            else if (daocha_1_1.定反位 == Single_Switch.DingFan.定位 && daocha_1_1.锁闭状态 == Single_Switch.STATE.锁闭)
            {
                q = "02";
            }
            else if (daocha_1_1.定反位 == Single_Switch.DingFan.定位 && daocha_1_1.锁闭状态 == Single_Switch.STATE.占用)
            {
                q = "03";
            }
            else if (daocha_1_1.定反位 == Single_Switch.DingFan.反位 && daocha_1_1.锁闭状态 == Single_Switch.STATE.空闲)
            {
                q = "04";
            }
            else if (daocha_1_1.定反位 == Single_Switch.DingFan.反位 && daocha_1_1.锁闭状态 == Single_Switch.STATE.锁闭)
            {
                q = "05";
            }
            else
            {
                q = "06";
            }

            //判断南京南DC2道轨的状态
            string r;
            if (daocha_1_2.定反位 == Single_Switch.DingFan.定位 && daocha_1_2.锁闭状态 == Single_Switch.STATE.空闲)
            {
                r = "01";
            }
            else if (daocha_1_2.定反位 == Single_Switch.DingFan.定位 && daocha_1_2.锁闭状态 == Single_Switch.STATE.锁闭)
            {
                r = "02";
            }
            else if (daocha_1_2.定反位 == Single_Switch.DingFan.定位 && daocha_1_2.锁闭状态 == Single_Switch.STATE.占用)
            {
                r = "03";
            }
            else if (daocha_1_2.定反位 == Single_Switch.DingFan.反位 && daocha_1_2.锁闭状态 == Single_Switch.STATE.空闲)
            {
                r = "04";
            }
            else if (daocha_1_2.定反位 == Single_Switch.DingFan.反位 && daocha_1_2.锁闭状态 == Single_Switch.STATE.锁闭)
            {
                r = "05";
            }
            else
            {
                r = "06";
            }

            //判断南京南DC3道轨的状态
            string s;
            if (daocha_1_3.定反位 == Single_Switch.DingFan.定位 && daocha_1_3.锁闭状态 == Single_Switch.STATE.空闲)
            {
                s = "01";
            }
            else if (daocha_1_3.定反位 == Single_Switch.DingFan.定位 && daocha_1_3.锁闭状态 == Single_Switch.STATE.锁闭)
            {
                s = "02";
            }
            else if (daocha_1_3.定反位 == Single_Switch.DingFan.定位 && daocha_1_3.锁闭状态 == Single_Switch.STATE.占用)
            {
                s = "03";
            }
            else if (daocha_1_3.定反位 == Single_Switch.DingFan.反位 && daocha_1_3.锁闭状态 == Single_Switch.STATE.空闲)
            {
                s = "04";
            }
            else if (daocha_1_3.定反位 == Single_Switch.DingFan.反位 && daocha_1_3.锁闭状态 == Single_Switch.STATE.锁闭)
            {
                s = "05";
            }
            else
            {
                s = "06";
            }

            //判断南京南DC4道轨的状态
            string t;
            if (daocha_1_4.定反位 == Single_Switch.DingFan.定位 && daocha_1_4.锁闭状态 == Single_Switch.STATE.空闲)
            {
                t = "01";
            }
            else if (daocha_1_4.定反位 == Single_Switch.DingFan.定位 && daocha_1_4.锁闭状态 == Single_Switch.STATE.锁闭)
            {
                t = "02";
            }
            else if (daocha_1_4.定反位 == Single_Switch.DingFan.定位 && daocha_1_4.锁闭状态 == Single_Switch.STATE.占用)
            {
                t = "03";
            }
            else if (daocha_1_4.定反位 == Single_Switch.DingFan.反位 && daocha_1_4.锁闭状态 == Single_Switch.STATE.空闲)
            {
                t = "04";
            }
            else if (daocha_1_4.定反位 == Single_Switch.DingFan.反位 && daocha_1_4.锁闭状态 == Single_Switch.STATE.锁闭)
            {
                t = "05";
            }
            else
            {
                t = "06";
            }
            Collect_Info = "AB" + "7F" + "01" + "11" + "05" + a + b + c + d + e + f + g + h + i + j + k + l + m + n + o + p + q + r + s + t + "AC";    
            return Collect_Info;
        }
        #endregion     

        #region  解析CTC发来的办理进路命令
        //建立哈希表用于站场控件的对应
        Hashtable ht_switch_1 = new Hashtable();
        Hashtable ht_track = new Hashtable();
        Hashtable ht_signal = new Hashtable();
        Hashtable ht_signal_drive = new Hashtable();

        //控件初始化函数 用于控件和编号之间的对应
        private void ControlsNumber_Init()
        {
            //单动道岔
            ht_switch_1.Add("07", daocha_1_1);
            ht_switch_1.Add("08", daocha_1_3);
            ht_switch_1.Add("09", daocha_1_2);
            ht_switch_1.Add("10", daocha_1_4);

            //双动道岔  南京南站没有，不写

            //出站信号机  
            ht_signal.Add("16", NJN_X1);
            ht_signal.Add("17", NJN_X2);
            ht_signal.Add("15", NJN_X3);
            ht_signal.Add("18", NJN_X4);
            ht_signal.Add("12", NJN_S1);
            ht_signal.Add("13", NJN_S2);
            ht_signal.Add("11", NJN_S3);
            ht_signal.Add("14", NJN_S4);

            //进站信号机
            ht_signal.Add("09", NJN_X);
            ht_signal.Add("10", NJN_XF);
            ht_signal.Add("19", NJN_SF);
            ht_signal.Add("20", NJN_S);
            
            //轨道
            ht_track.Add("04", dangui_1);
            ht_track.Add("05", dangui_3);
            ht_track.Add("06", dangui_5);
            ht_track.Add("19", dangui_6);
            ht_track.Add("20", dangui_4);
            ht_track.Add("21", dangui_2);
            ht_track.Add("07", dangui_7);
            ht_track.Add("08", dangui_9);
            ht_track.Add("09", dangui_11);
            ht_track.Add("16", dangui_8);
            ht_track.Add("17", dangui_10);
            ht_track.Add("18", dangui_12);
            ht_track.Add("39", Track_G3);
            ht_track.Add("40", Track_G1);
            ht_track.Add("41", Track_G2);
            ht_track.Add("42", Track_G4);

            ht_signal_drive.Add(NJN_X, 0);
            ht_signal_drive.Add(NJN_XF, 1);
            ht_signal_drive.Add(NJN_S1, 2);
            ht_signal_drive.Add(NJN_S2, 3);
            ht_signal_drive.Add(NJN_S3, 4);
            ht_signal_drive.Add(NJN_S4, 5);
            ht_signal_drive.Add(NJN_S, 6);
            ht_signal_drive.Add(NJN_SF, 7);
            ht_signal_drive.Add(NJN_X1, 8);
            ht_signal_drive.Add(NJN_X2, 9);
            ht_signal_drive.Add(NJN_X3, 10);
            ht_signal_drive.Add(NJN_X4, 11);
        }

        private void RouteHandle_fromCTC()
        {
            if (Client_Rec.Substring(0, 4) == "AB8F" && Client_Rec.Length == 36)
            {
                if (Client_Rec.Substring(6, 4) == "0511")
                {
                    //单动道岔1                               
                    if (Client_Rec.Substring(14, 4) == "0702")
                    {                                             
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[0], "D");
                        daocha_1_1.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(14, 4) == "0705")
                    {                                              
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[0], "F");
                        daocha_1_1.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(14, 4) == "0802")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[1], "D");
                        daocha_1_3.锁闭状态 = Single_Switch.STATE.锁闭;                                                           
                    }
                    else if (Client_Rec.Substring(14, 4) == "0805")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[1], "F");
                        daocha_1_3.锁闭状态 = Single_Switch.STATE.锁闭;                                                        
                    }
                    else if (Client_Rec.Substring(14, 4) == "0902")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[2], "D");                      
                        daocha_1_2.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(14, 4) == "0905")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[2], "F");                  
                        daocha_1_2.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(14, 4) == "1002")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[2], "D");                     
                        daocha_1_4.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(14, 4) == "1005")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[3], "F");                        
                        daocha_1_4.锁闭状态 = Single_Switch.STATE.锁闭;
                    }

                    //单动道岔2             
                    if (Client_Rec.Substring(18, 4) == "0702")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[0], "D");
                        daocha_1_1.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(18, 4) == "0705")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[0], "F");                     
                        daocha_1_1.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(18, 4) == "0802")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[1], "D");                     
                        daocha_1_3.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(18, 4) == "0805")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[1], "F");                   
                        daocha_1_3.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(18, 4) == "0902")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[2], "D");                   
                        daocha_1_2.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(18, 4) == "0905")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[2], "F");                   
                        daocha_1_2.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(18, 4) == "1002")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[3], "D");                    
                        daocha_1_4.锁闭状态 = Single_Switch.STATE.锁闭;
                    }
                    else if (Client_Rec.Substring(18, 4) == "1005")
                    {
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[3], "F");                
                        daocha_1_4.锁闭状态 = Single_Switch.STATE.锁闭;
                    }                   

                    //股道
                    if (Client_Rec.Substring(28, 2) == "01")
                    {
                        ((Track)ht_track[Client_Rec.Substring(26, 2)]).flag_zt = 3;
                        ((Track)ht_track[Client_Rec.Substring(26, 2)]).Drawpic();
                    }
                    else if (Client_Rec.Substring(28, 2) == "02")
                    {
                        ((Track)ht_track[Client_Rec.Substring(26, 2)]).flag_zt = 2;
                        ((Track)ht_track[Client_Rec.Substring(26, 2)]).Drawpic();
                    }
                    else if (Client_Rec.Substring(28, 2) == "03")
                    {
                        ((Track)ht_track[Client_Rec.Substring(26, 2)]).flag_zt = 1;
                        ((Track)ht_track[Client_Rec.Substring(26, 2)]).Drawpic();
                    }

                    //信号机                
                    if (Client_Rec.Substring(32, 2) == "01")
                    {
                        ((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)]).X_flag = 5;
                        ((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)]).drawpic();
                        Route_Signal_Drive(Drive_Collect.Nanjing_name_CH365_Signal[(int)ht_signal_drive[((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)])]], "H");
                    }
                    else if (Client_Rec.Substring(32, 2) == "02")
                    {
                        ((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)]).X_flag = 3;
                        ((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)]).drawpic();
                        Route_Signal_Drive(Drive_Collect.Nanjing_name_CH365_Signal[(int)ht_signal_drive[((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)])]], "L");
                    }
                    else if (Client_Rec.Substring(32, 2) == "03")
                    {
                        ((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)]).X_flag = 1;
                        ((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)]).drawpic();
                        Route_Signal_Drive(Drive_Collect.Nanjing_name_CH365_Signal[(int)ht_signal_drive[((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)])]], "U");
                    }
                    else if (Client_Rec.Substring(32, 2) == "04")
                    {
                        ((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)]).X_flag = 2;
                        ((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)]).drawpic();
                        Route_Signal_Drive(Drive_Collect.Nanjing_name_CH365_Signal[(int)ht_signal_drive[((Train_Signal)ht_signal[Client_Rec.Substring(30, 2)])]], "UU");
                    }
                }
                else
                {
                    MessageBox.Show("进路信息有误！");
                }
            }
        }
        #endregion

        #region 道岔单操
        //道岔单操（单独操作）功能按钮函数
        private void btnFuction_Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs)(e)).Button == MouseButtons.Left)
            {
                string str = ((Button)(sender)).Name;
                if (list_Xbutton_Switch.Contains(str))
                {
                    MessageBox.Show("已选中");
                }
                else
                {
                    list_Xbutton_Switch.Add(((Button)(sender)).Name);
                }
            }
        }


        

        /// <summary>
        /// 对道岔1的单操
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Daocha1_1_Click_1(object sender, EventArgs e)
        {
            if(CBI_state.Self_control_mode)
            {
                list_Xbutton_Switch.Add(((Button)sender).Name);
                daocha_1_1.dancao = Single_Switch.DanCao.单操;
                string str = "";
                foreach (var item in list_Xbutton_Switch)
                {
                    if (str != "")
                    {
                        str = str + "+" + item;//功能按钮加上道岔1的按钮
                    }
                    else
                    {
                        str = item;//存储功能按钮
                    }

                }
                string first_button = str.Split('+')[0];
                if (first_button == "btn_XNormalPosition")   //总定位按钮
                {
                    if (daocha_1_1.state == Single_Switch.STATE.空闲)
                    {
                        btn_Daocha1_1.BackColor = Color.Green;
                        daocha_1_1.定反位 = Single_Switch.DingFan.定位;
                        ht_SwitchState[daocha_1_1] = 0;//状态为0
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[0], "D");
                    }
                    else if (daocha_1_1.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔1处于锁闭状态，不能转换为定位状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔区段处于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_XReversePosition")//总反位
                {
                    if (daocha_1_1.state == Single_Switch.STATE.空闲)
                    {
                        btn_Daocha1_1.BackColor = Color.Yellow;
                        daocha_1_1.定反位 = Single_Switch.DingFan.反位;
                        ht_SwitchState[daocha_1_1] = -1;//状态为反位
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[0], "F");
                    }
                    else if (daocha_1_1.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔1处于锁闭状态，不能转换为反位状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔区段处于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_XSingleLock")   //单锁(单独锁闭)按钮
                {
                    if (daocha_1_1.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔1已经锁闭！");
                    }
                    else
                    {
                        //判断是否已经单封
                        if (daocha_1_1.state == Single_Switch.STATE.空闲 && label111.BackColor != Color.Purple)//无锁闭，无单封
                        {
                            daocha_1_1.state = Single_Switch.STATE.锁闭;
                            ht_SwitchState[daocha_1_1] = 2;//状态为锁闭状态
                            label111.BackColor = Color.Red;
                            btn_dc1.BackColor = Color.Red;
                            btn_dc1.Visible = true;
                            daocha_1_1.draw();
                        }
                        else if (daocha_1_1.state == Single_Switch.STATE.空闲 && label111.BackColor == Color.Purple)//无锁闭，有单封
                        {
                            MessageBox.Show("道岔1处于单封状态");
                        }
                    }
                }

                else if (first_button == "btn_XSingleUnlock")   //单解按钮
                {
                    if (daocha_1_1.state == Single_Switch.STATE.锁闭)
                    {
                        daocha_1_1.state = Single_Switch.STATE.空闲;
                        ht_SwitchState[daocha_1_1] = 1;//状态为常态
                        daocha_1_1.dancao = Single_Switch.DanCao.进路;
                        label111.BackColor = Color.Black;
                        btn_dc1.Visible = false;
                        btn_dc1.BackColor = Color.White;
                        daocha_1_1.draw();
                    }
                    else
                    {
                        MessageBox.Show("道岔1没有锁闭！");
                    }
                }

                else if (first_button == "btn_XLock")   //封锁（封闭道岔）按钮，不准排列经过此道岔的进路，一般用于道岔清扫和维修
                {
                    if (daocha_1_1.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔1已经锁闭！");
                    }
                    else if (daocha_1_1.state == Single_Switch.STATE.空闲)
                    {
                        daocha_1_1.state = Single_Switch.STATE.空闲;
                        ht_SwitchState[daocha_1_1] = 3;//单封状态
                        label111.BackColor = Color.Purple;
                        btn_dc1.BackColor = Color.Purple;
                        btn_dc1.Visible = true;
                        daocha_1_1.draw();
                    }
                    else
                    {
                        MessageBox.Show("道岔区段出于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_XUnlock")//解封
                {
                    if (daocha_1_1.state == Single_Switch.STATE.空闲 && label111.BackColor == Color.Purple)//出于单封状态
                    {
                        daocha_1_1.state = Single_Switch.STATE.空闲;
                        ht_SwitchState[daocha_1_1] = 1;//状态为常态
                        label111.BackColor = Color.Black;
                        btn_dc1.Visible = false;
                        btn_dc1.BackColor = Color.White;
                        daocha_1_1.draw();
                    }
                    else if (daocha_1_1.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔1处于锁闭状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔1未单封");
                    }
                }
                list_Xbutton_Switch.Clear();

            }//模式的if
            else if (CBI_state.Self_to_CTC_mode)
            {
                MessageBox.Show("自律模式不可单操");
            }
            else
            {
                MessageBox.Show("非法操作！");
            }

        }
       

        /// <summary>
        /// 道岔3的单操
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Daocha1_3_Click_1(object sender, EventArgs e)
        {
            if(CBI_state.Self_control_mode)
            {
                list_Xbutton_Switch.Add(((Button)sender).Name);
                daocha_1_3.dancao = Single_Switch.DanCao.单操;
                string str = "";
                foreach (var item in list_Xbutton_Switch)
                {
                    if (str != "")
                    {
                        str = str + "+" + item;//功能按钮加上道岔1的按钮
                    }
                    else
                    {
                        str = item;//存储功能按钮
                    }

                }
                string first_button = str.Split('+')[0];
                if (first_button == "btn_XNormalPosition")   //总定位按钮
                {
                    if (daocha_1_3.state == Single_Switch.STATE.空闲)
                    {
                        btn_Daocha1_3.BackColor = Color.Green;
                        daocha_1_3.定反位 = Single_Switch.DingFan.定位;
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[1], "D");
                    }
                    else if (daocha_1_1.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔3处于锁闭状态，不能转换为定位状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔区段处于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_XReversePosition")//总反位
                {
                    if (daocha_1_3.state == Single_Switch.STATE.空闲)
                    {
                        btn_Daocha1_3.BackColor = Color.Yellow;
                        daocha_1_3.定反位 = Single_Switch.DingFan.反位;
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[1], "F");
                    }
                    else if (daocha_1_3.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔3处于锁闭状态，不能转换为反位状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔区段处于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_XSingleLock")   //单锁(单独锁闭)按钮
                {
                    if (daocha_1_3.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔3已经锁闭！");
                    }
                    else
                    {
                        //判断是否已经单封
                        if (daocha_1_3.state == Single_Switch.STATE.空闲 && label113.BackColor != Color.Purple)//无锁闭，无单封
                        {
                            daocha_1_3.state = Single_Switch.STATE.锁闭;
                            ht_SwitchState[daocha_1_3] = 2;//状态为锁闭状态
                            label113.BackColor = Color.Red;
                            btn_dc3.BackColor = Color.Red;
                            btn_dc3.Visible = true;
                            daocha_1_3.draw();
                        }
                        else if (daocha_1_3.state == Single_Switch.STATE.空闲 && label113.BackColor == Color.Purple)//无锁闭，有单封
                        {
                            MessageBox.Show("道岔3处于单封状态");
                        }
                    }
                }

                else if (first_button == "btn_XSingleUnlock")   //单解按钮
                {
                    if (daocha_1_3.state == Single_Switch.STATE.锁闭)
                    {
                        daocha_1_3.state = Single_Switch.STATE.空闲;
                        daocha_1_3.dancao = Single_Switch.DanCao.进路;
                        ht_SwitchState[daocha_1_3] = 1;//状态为空闲状态
                        label113.BackColor = Color.Black;
                        btn_dc3.Visible = false;
                        btn_dc3.BackColor = Color.White;
                        daocha_1_3.draw();
                    }
                    else
                    {
                        MessageBox.Show("道岔3没有锁闭！");
                    }
                }

                else if (first_button == "btn_XLock")   //封锁（封闭道岔）按钮，不准排列经过此道岔的进路，一般用于道岔清扫和维修
                {
                    if (daocha_1_3.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔3已经锁闭！");
                    }
                    else if (daocha_1_3.state == Single_Switch.STATE.空闲)
                    {
                        daocha_1_3.state = Single_Switch.STATE.空闲;
                        ht_SwitchState[daocha_1_3] = 1;//状态为单封状态
                        label113.BackColor = Color.Purple;
                        btn_dc3.BackColor = Color.Purple;
                        btn_dc3.Visible = true;
                        daocha_1_3.draw();
                    }
                    else
                    {
                        MessageBox.Show("道岔区段出于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_XUnlock")//解封
                {
                    if (daocha_1_3.state == Single_Switch.STATE.空闲 && label113.BackColor == Color.Purple)//出于单封状态
                    {
                        daocha_1_3.state = Single_Switch.STATE.空闲;
                        ht_SwitchState[daocha_1_3] = 1;//状态为解封状态
                        label113.BackColor = Color.Black;
                        btn_dc3.Visible = false;
                        btn_dc3.BackColor = Color.White;
                        daocha_1_3.draw();
                    }
                    else if (daocha_1_3.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔3处于锁闭状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔3未单封");
                    }
                }
                list_Xbutton_Switch.Clear();

            }//模式下的if
            else if(CBI_state.Self_to_CTC_mode)
            {
                MessageBox.Show("自律模式下不可单操");
            }
            else
            {
                MessageBox.Show("非法操作！");
            }
            
        }
        /// <summary>
        /// 道岔2的单操
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Daocha1_2_Click_1(object sender, EventArgs e)
        {
            if(CBI_state.Self_control_mode)
            {
                list_Xbutton_Switch.Add(((Button)sender).Name);
                daocha_1_2.dancao = Single_Switch.DanCao.单操;
                string str = "";
                foreach (var item in list_Xbutton_Switch)
                {
                    if (str != "")
                    {
                        str = str + "+" + item;//功能按钮加上道岔1的按钮
                    }
                    else
                    {
                        str = item;//存储功能按钮
                    }

                }
                string first_button = str.Split('+')[0];
                if (first_button == "btn_SNormalPosition")   //总定位按钮
                {
                    if (daocha_1_2.state == Single_Switch.STATE.空闲)
                    {
                        btn_Daocha1_2.BackColor = Color.Green;
                        daocha_1_2.定反位 = Single_Switch.DingFan.定位;
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[2], "D");
                    }
                    else if (daocha_1_2.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔2处于锁闭状态，不能转换为定位状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔区段处于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_SReversePosition")//总反位
                {
                    if (daocha_1_2.state == Single_Switch.STATE.空闲)
                    {
                        btn_Daocha1_2.BackColor = Color.Yellow;
                        daocha_1_2.定反位 = Single_Switch.DingFan.反位;
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[2], "F");
                    }
                    else if (daocha_1_2.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔2处于锁闭状态，不能转换为反位状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔区段处于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_SSingleLock")   //单锁(单独锁闭)按钮
                {
                    if (daocha_1_2.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔2已经锁闭！");
                    }
                    else
                    {
                        //判断是否已经单封
                        if (daocha_1_2.state == Single_Switch.STATE.空闲 && label112.BackColor != Color.Purple)//无锁闭，无单封
                        {
                            daocha_1_2.state = Single_Switch.STATE.锁闭;
                            ht_SwitchState[daocha_1_2] = 2;//状态为锁闭状态
                            label112.BackColor = Color.Red;
                            btn_dc2.BackColor = Color.Red;
                            btn_dc2.Visible = true;
                            daocha_1_2.draw();
                        }
                        else if (daocha_1_2.state == Single_Switch.STATE.空闲 && label112.BackColor == Color.Purple)//无锁闭，有单封
                        {
                            MessageBox.Show("道岔2处于单封状态");
                        }
                    }
                }

                else if (first_button == "btn_SSingleUnlock")   //单解按钮
                {
                    if (daocha_1_2.state == Single_Switch.STATE.锁闭)
                    {
                        daocha_1_2.state = Single_Switch.STATE.空闲;
                        daocha_1_2.dancao = Single_Switch.DanCao.进路;
                        ht_SwitchState[daocha_1_2] = 1;//状态为解锁状态
                        label112.BackColor = Color.Black;
                        btn_dc2.Visible = false;
                        btn_dc2.BackColor = Color.White;
                        daocha_1_2.draw();
                    }
                    else
                    {
                        MessageBox.Show("道岔2没有锁闭！");
                    }
                }

                else if (first_button == "btn_SLock")   //封锁（封闭道岔）按钮，不准排列经过此道岔的进路，一般用于道岔清扫和维修
                {
                    if (daocha_1_2.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔2已经锁闭！");
                    }
                    else if (daocha_1_2.state == Single_Switch.STATE.空闲)
                    {
                        daocha_1_2.state = Single_Switch.STATE.空闲;
                        ht_SwitchState[daocha_1_2] = 3;//状态为单封状态
                        label112.BackColor = Color.Purple;
                        btn_dc2.BackColor = Color.Purple;
                        btn_dc2.Visible = true;
                        daocha_1_2.draw();
                    }
                    else
                    {
                        MessageBox.Show("道岔区段出于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_SUnlock")//解封
                {
                    if (daocha_1_2.state == Single_Switch.STATE.空闲 && label112.BackColor == Color.Purple)//出于单封状态
                    {
                        daocha_1_2.state = Single_Switch.STATE.空闲;
                        ht_SwitchState[daocha_1_2] = 1;//状态为解封状态
                        label112.BackColor = Color.Black;
                        btn_dc2.Visible = false;
                        btn_dc2.BackColor = Color.White;
                        daocha_1_2.draw();
                    }
                    else if (daocha_1_2.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔2处于锁闭状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔2未单封");
                    }
                }
                list_Xbutton_Switch.Clear();
            }//模式下的if 
            else if (CBI_state.Self_to_CTC_mode)
            {
                MessageBox.Show("自律模式下不可单操");
            }
            else
            {
                MessageBox.Show("非法操作！");
            }
            
        }
     
        /// <summary>
        /// 道岔4的单操
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Daocha1_4_Click_1(object sender, EventArgs e)
        {
            if(CBI_state.Self_control_mode)
            {
                list_Xbutton_Switch.Add(((Button)sender).Name);
                daocha_1_4.dancao = Single_Switch.DanCao.单操;
                string str = "";
                foreach (var item in list_Xbutton_Switch)
                {
                    if (str != "")
                    {
                        str = str + "+" + item;//功能按钮加上道岔1的按钮
                    }
                    else
                    {
                        str = item;//存储功能按钮
                    }

                }
                string first_button = str.Split('+')[0];
                if (first_button == "btn_SNormalPosition")   //总定位按钮
                {
                    if (daocha_1_4.state == Single_Switch.STATE.空闲)
                    {
                        btn_Daocha1_4.BackColor = Color.Green;
                        daocha_1_4.定反位 = Single_Switch.DingFan.定位;
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[3], "D");
                    }
                    else if (daocha_1_4.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔4处于锁闭状态，不能转换为定位状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔区段处于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_SReversePosition")//总反位
                {
                    if (daocha_1_4.state == Single_Switch.STATE.空闲)
                    {
                        btn_Daocha1_4.BackColor = Color.Yellow;
                        daocha_1_4.定反位 = Single_Switch.DingFan.反位;
                        Route_Switch_Drive(Drive_Collect.Nanjing_name_CH365_Switch[3], "F");
                    }
                    else if (daocha_1_4.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔4处于锁闭状态，不能转换为反位状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔区段处于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_SSingleLock")   //单锁(单独锁闭)按钮
                {
                    if (daocha_1_4.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔4已经锁闭！");
                    }
                    else
                    {
                        //判断是否已经单封
                        if (daocha_1_4.state == Single_Switch.STATE.空闲 && label114.BackColor != Color.Purple)//无锁闭，无单封
                        {
                            daocha_1_4.state = Single_Switch.STATE.锁闭;
                            ht_SwitchState[daocha_1_4] = 2;//状态为锁闭状态
                            label114.BackColor = Color.Red;
                            btn_dc4.BackColor = Color.Red;
                            btn_dc4.Visible = true;
                            daocha_1_4.draw();
                        }
                        else if (daocha_1_4.state == Single_Switch.STATE.空闲 && label114.BackColor == Color.Purple)//无锁闭，有单封
                        {
                            MessageBox.Show("道岔4处于单封状态");
                        }
                    }
                }

                else if (first_button == "btn_SSingleUnlock")   //单解按钮
                {
                    if (daocha_1_4.state == Single_Switch.STATE.锁闭)
                    {
                        daocha_1_4.state = Single_Switch.STATE.空闲;
                        ht_SwitchState[daocha_1_4] = 1;//状态为解锁状态
                        daocha_1_4.dancao = Single_Switch.DanCao.进路;
                        label114.BackColor = Color.Black;
                        btn_dc4.Visible = false;
                        btn_dc4.BackColor = Color.White;
                        daocha_1_4.draw();
                    }
                    else
                    {
                        MessageBox.Show("道岔4没有锁闭！");
                    }
                }

                else if (first_button == "btn_SLock")   //封锁（封闭道岔）按钮，不准排列经过此道岔的进路，一般用于道岔清扫和维修
                {
                    if (daocha_1_4.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔4已经锁闭！");
                    }
                    else if (daocha_1_4.state == Single_Switch.STATE.空闲)
                    {
                        daocha_1_4.state = Single_Switch.STATE.空闲;
                        ht_SwitchState[daocha_1_4] = 3;//状态为锁闭状态
                        label114.BackColor = Color.Purple;
                        btn_dc4.BackColor = Color.Purple;
                        btn_dc4.Visible = true;
                        daocha_1_4.draw();
                    }
                    else
                    {
                        MessageBox.Show("道岔区段出于占用状态，不可单操");
                    }
                }

                else if (first_button == "btn_SUnlock")//解封
                {
                    if (daocha_1_4.state == Single_Switch.STATE.空闲 && label114.BackColor == Color.Purple)//出于单封状态
                    {
                        daocha_1_4.state = Single_Switch.STATE.空闲;
                        ht_SwitchState[daocha_1_4] = 1;//状态为解封状态
                        label114.BackColor = Color.Black;
                        btn_dc4.Visible = false;
                        btn_dc4.BackColor = Color.White;
                        daocha_1_4.draw();
                    }
                    else if (daocha_1_4.state == Single_Switch.STATE.锁闭)
                    {
                        MessageBox.Show("道岔4处于锁闭状态！");
                    }
                    else
                    {
                        MessageBox.Show("道岔4未单封");
                    }
                }
                list_Xbutton_Switch.Clear();
            }//模式下的if
            else  if (CBI_state.Self_to_CTC_mode)
            {
                MessageBox.Show("自律模式下不可单操！");
            }
            else
            {
                MessageBox.Show("非法操作！");
            }
        }
      
        #endregion

        #region  引导接车
        private void btn_XAllLock_Click(object sender, EventArgs e)
        {
            ExchangeValue.SorX = true;
            PswForm fs = new PswForm();
            fs.Show();
        }

        private void btn_SAllLock_Click(object sender, EventArgs e)
        {
            ExchangeValue.SorX = false;
            PswForm fs = new PswForm();
            fs.Show();
        }

        public void XYinDao()//下行引导接车
        {
            daocha_1_1.dancao = Single_Switch.DanCao.单操;
            daocha_1_1.state = Single_Switch.STATE.锁闭;
            label111.BackColor = Color.Red;
            btn_dc1.BackColor = Color.Red;
            btn_dc1.Visible = true;

            daocha_1_3.dancao = Single_Switch.DanCao.单操;
            daocha_1_3.state = Single_Switch.STATE.锁闭;
            label113.BackColor = Color.Red;
            btn_dc3.BackColor = Color.Red;
            btn_dc3.Visible = true;

            //信号机显示红白
            NJN_X.drawpic(7);
            Route_Signal_Drive("NJN_X", "HB");



        }
        public void SYindao()//上行引导接车
        {
            daocha_1_2.dancao = Single_Switch.DanCao.单操;
            daocha_1_2.state = Single_Switch.STATE.锁闭;
            label112.BackColor = Color.Red;
            btn_dc2.BackColor = Color.Red;
            btn_dc2.Visible = true;

            daocha_1_4.dancao = Single_Switch.DanCao.单操;
            daocha_1_4.state = Single_Switch.STATE.锁闭;
            label114.BackColor = Color.Red;
            btn_dc4.BackColor = Color.Red;
            btn_dc4.Visible = true;

            NJN_S.drawpic(7);
            Route_Signal_Drive("NJN_S", "HB");

        }
        public void XYinDaoCancle()//下行引导接车解锁
        {

            daocha_1_1.dancao = Single_Switch.DanCao.进路;
            daocha_1_1.state = Single_Switch.STATE.空闲;
            label111.BackColor = Color.Black;
            btn_dc1.BackColor = Color.Black;
            btn_dc1.Visible = false;

            daocha_1_3.dancao = Single_Switch.DanCao.进路;
            daocha_1_3.state = Single_Switch.STATE.空闲;
            label113.BackColor = Color.Black;
            btn_dc3.BackColor = Color.Black;
            btn_dc3.Visible = false;

            NJN_X.drawpic(5);
            Route_Signal_Drive("NJN_X", "H");

        }
        public void SYinDaoCancle()//上行引导接车解锁
        {
            daocha_1_2.dancao = Single_Switch.DanCao.进路;
            daocha_1_2.state = Single_Switch.STATE.空闲;
            label112.BackColor = Color.Black;
            btn_dc2.BackColor = Color.Black;
            btn_dc2.Visible = false;

            daocha_1_4.dancao = Single_Switch.DanCao.进路;
            daocha_1_4.state = Single_Switch.STATE.空闲;
            label114.BackColor = Color.Black;
            btn_dc4.BackColor = Color.Black;
            btn_dc4.Visible = false;

            NJN_S.drawpic(5);
            Route_Signal_Drive("NJN_S", "H");
        }

        private void btn_YD_Click(object sender, EventArgs e)
        {
            PswForm fs = new PswForm();
            fs.Show();
        }

        private void btn_YD_S_Click(object sender, EventArgs e)
        {
            PswForm fs = new PswForm();
            fs.Show();
        }
        #endregion

        #region 按钮功能函数
        //系统第一次启动后解锁整个站场
        private void btn_PowerUnlock_Click(object sender, EventArgs e)
        {
            myTopMost.Show();
        }

        //显示/隐藏所有信号名称
        private void btn_SignalName_Click(object sender, EventArgs e)
        {
            btn_Signal_Count++;
            if (btn_Signal_Count % 2 == 0)
            {
                lbl_X.Show();
                lbl_XI.Show();
                lbl_XII.Show();
                lbl_X3.Show();
                lbl_X4.Show();
                lbl_XF.Show();
                lbl_S.Show();
                lbl_SI.Show();
                lbl_SII.Show();
                lbl_S3.Show();
                lbl_S4.Show();
                lbl_SF.Show();
             
            }
            if (btn_Signal_Count % 2 == 1)
            {
                lbl_X.Hide();
                lbl_XI.Hide();
                lbl_XII.Hide();
                lbl_X3.Hide();
                lbl_X4.Hide();
                lbl_XF.Hide();
                lbl_S.Hide();
                lbl_SI.Hide();
                lbl_SII.Hide();
                lbl_S3.Hide();
                lbl_S4.Hide();
                lbl_SF.Hide();
            }
        }

        public void labelshow()
        {
            lbl_IG.Show();
            lbl_2G.Show();
            lbl_3G.Show();
            lbl_4G.Show();
            lbl_S1JG.Show();
            lbl_S2JG.Show();
            lbl_S3JG.Show();
            lbl_S1LQ.Show();
            lbl_S2LQ.Show();
            lbl_S3LQ.Show();
            lbl_X1JG.Show();
            lbl_X2JG.Show();
            lbl_X3JG.Show();
            lbl_X1LQ.Show();
            lbl_X2LQ.Show();
            lbl_X3LQ.Show();
            label_1DG.Show();
            label_2DG.Show();
            label_3DG.Show();
            label_4DG.Show();
        }
        //显示/隐藏所有道岔名称
        private void btn_SwitchName_Click(object sender, EventArgs e)
        {
            btn_Switch_Count++;
            if (btn_Switch_Count % 2 == 0)
            {
                lbl_dc1.Show();
                lbl_dc2.Show();
                lbl_dc3.Show();
                lbl_dc4.Show();
            }
            if (btn_Switch_Count % 2 == 1)
            {
                lbl_dc1.Hide();
                lbl_dc2.Hide();
                lbl_dc3.Hide();
                lbl_dc4.Hide();
            }
        }

        //显示/隐藏所有区段名称
        private void btn_TrkName_Click(object sender, EventArgs e)
        {
            btn_Track_Count++;
            if (btn_Track_Count % 2 == 0)
            {
                lbl_IG.Show();
                lbl_2G.Show();
                lbl_3G.Show();
                lbl_4G.Show();
                lbl_S1JG.Show();
                lbl_S2JG.Show();
                lbl_S3JG.Show();
                lbl_S1LQ.Show();
                lbl_S2LQ.Show();
                lbl_S3LQ.Show();
                lbl_X1JG.Show();
                lbl_X2JG.Show();
                lbl_X3JG.Show();
                lbl_X1LQ.Show();
                lbl_X2LQ.Show();
                lbl_X3LQ.Show();
                label_1DG.Show();
                label_2DG.Show();
                label_3DG.Show();
                label_4DG.Show();
            }
            else
            {
                lbl_IG.Hide();
                lbl_2G.Hide();
                lbl_3G.Hide();
                lbl_4G.Hide();
                lbl_S1JG.Hide();
                lbl_S2JG.Hide();
                lbl_S3JG.Hide();
                lbl_S1LQ.Hide();
                lbl_S2LQ.Hide();
                lbl_S3LQ.Hide();
                lbl_X1JG.Hide();
                lbl_X2JG.Hide();
                lbl_X3JG.Hide();
                lbl_X1LQ.Hide();
                lbl_X2LQ.Hide();
                lbl_X3LQ.Hide();
                label_1DG.Hide();
                label_2DG.Hide();
                label_3DG.Hide();
                label_4DG.Hide();
            }
        }

        private void btn_Help_Click(object sender, EventArgs e)
        {
            btn_Help_Count++;
            if (btn_Help_Count % 2 == 0)
            {
                btn_dc1.Show();
                btn_dc2.Show();
                btn_dc3.Show();
                btn_dc4.Show();
            }
            else
            {
                btn_dc1.Hide();
                btn_dc2.Hide();
                btn_dc3.Hide();
                btn_dc4.Hide();
            }
        }
        #endregion

        #region CH365信息读取写入
        /// <summary>
        /// 打开设备,2块驱动采集板
        /// </summary>
        private void Open_Ch365_IO()
        {
            Ch365_Class.CH365mOpenDevice(0, true, true);
            Ch365_Class.CH365mOpenDevice(1, true, true);        
        }
        /// <summary>
        /// 读取板卡中的数据
        /// </summary>
        /////b : 采集板的板卡号,共2块（0,1）；p : 第p个8位，共4个 (0,1,2,3)；j : 位数，共8位(0,1,2,3,4,5,6,7)
        private void Read_Ch365_IO()
        {
            Ch365_Class._CH365_IO_REG ch365 = new Ch365_Class._CH365_IO_REG();
            Int32[] a = new Int32[1];
            for (int b = 0; b < 2; b++)
            {
                for (int p = 0; p < 4; p++)
                {
                    //读取板卡0中的数据（区段）
                    Ch365_Class.CH365mReadIoByte(b, ch365.mCh365IoPort + p, a);
                    Drive_Collect.Collect_Data_CH365_IO[b, p] = a[0];

                    #region 板卡数据分配
                    for (int j = 0; j < 4; j++)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Drive_Collect.Collect_Split_Data_CH365_IO[b, j, i] = Drive_Collect.Collect_Data_CH365_IO[b, j] >> i & 1;
                        }
                    }
                    #endregion

                }
            }
        }
        /// <summary>
        /// 写入板卡中的数据
        /// </summary>
        ///  //b : 驱动板的板卡号,共4块（0,1,2,3）；p : 第p个8位，共4个 (0,1,2,3)；j : 位数，共8位(0,1,2,3,4,5,6,7)
        private void Write_Ch365_IO()
        {
            for (int b = 0; b < 4; b++)
            {
                for (int p = 0; p < 4; p++)
                {
                    Drive_Collect.Drive_Data_CH365_IO[b, p] = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        Drive_Collect.Drive_Data_CH365_IO[b, p] = Drive_Collect.Drive_Data_CH365_IO[b, p] + Drive_Collect.Drive_Split_Data_CH365_IO[b, p, j] * (int)Math.Pow(2, j);
                    }

                    Ch365_Class.CH365mWriteIoByte(b, 0x10 + p, Drive_Collect.Drive_Data_CH365_IO[b, p]);
                }
            }
        }
        #endregion

        #region 通过控件名获取控件
        private Control GetPbControl(string strName)
        {
            string pbName = strName;
            return GetControl(this, pbName);
        }    

        /// <summary>
        /// 通过控件名获取控件
        /// </summary>
        /// <param name="ct">控件所在的容器或者窗体</param>
        /// <param name="name">需要查找的控件名</param>
        /// <returns></returns>
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

        #endregion

        #region  区端故障解锁
        /// <summary>
        /// 区端故障解锁
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        
        private void MounseEnter(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void MounseLeave(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }
        string qgbutton = null;
        private void btn_XTrkUnlock_Click(object sender, EventArgs e)
        {
            //区段故障解锁按钮
            //主要实现的功能：实现断电后出现的白光带的解锁以及现场出现总人解和总取消不能解锁的情况下进行的一种强制的解锁方式
            //说明：此种解锁方式安全性不高，需要在现场人员确定的情况下方可实施
            //操作流程：点击区故解+输入口令+点击对应的轨道区段的名称实现解锁
            if (ExchangeValue.QGnumber == 0)
            {
                PswForm fs = new PswForm();
                fs.Show();
            }
            qgbutton = ((Button)sender).Name;
        }

        private void label_MouseClick(object sender, MouseEventArgs e)
        {
            qgbutton += ((Label)sender).Name;//实现双按钮解锁
            //此处实现获取轨道的名称，从而作用每个股道区段
            if (ExchangeValue.QG == "区故解")
            {
                if (qgbutton == "btn_XTrkUnlocklbl_IG")
                {

                    if (((Track)ht_Track["Track_G1"]).flag_zt == 2)//2==锁闭
                    {
                        ((Track)ht_Track["Track_G1"]).flag_zt = 3;//3==空闲
                        ((Track)ht_Track["Track_G1"]).Drawpic();
                    }

                }
                else if (qgbutton == "btn_XTrkUnlocklbl_2G")
                {
                    if (((Track)ht_Track["Track_G2"]).flag_zt == 2)
                    {
                        ((Track)ht_Track["Track_G2"]).flag_zt = 3;
                        ((Track)ht_Track["Track_G2"]).Drawpic();
                    }

                }
                else if (qgbutton == "btn_XTrkUnlocklbl_3G")
                {
                    if (((Track)ht_Track["Track_G3"]).flag_zt == 2)
                    {
                        ((Track)ht_Track["Track_G3"]).flag_zt = 3;
                        ((Track)ht_Track["Track_G3"]).Drawpic();
                    }

                }
                else if (qgbutton == "btn_XTrkUnlocklbl_4G")
                {
                    if (((Track)ht_Track["Track_G4"]).flag_zt == 2)
                    {
                        ((Track)ht_Track["Track_G4"]).flag_zt = 3;
                        ((Track)ht_Track["Track_G4"]).Drawpic();
                    }

                }
                else if (qgbutton == "btn_XTrkUnlocklabel_1DG")
                {
                    if (daocha_1_1.state == Single_Switch.STATE.锁闭)
                    {
                        daocha_1_1.state = Single_Switch.STATE.空闲;
                        daocha_1_1.draw();
                    }
                }
                else if (qgbutton == "btn_XTrkUnlocklabel_3DG")
                {
                    if (daocha_1_3.state == Single_Switch.STATE.锁闭)
                    {
                        daocha_1_3.state = Single_Switch.STATE.空闲;
                        daocha_1_3.draw();
                    }
                }
                else if (qgbutton == "btn_XTrkUnlocklabel_2DG")
                {
                    if (daocha_1_2.state == Single_Switch.STATE.锁闭)
                    {
                        daocha_1_2.state = Single_Switch.STATE.空闲;
                        daocha_1_2.draw();
                    }
                }
                else if (qgbutton == "btn_XTrkUnlocklabel_4DG")
                {
                    if (daocha_1_4.state == Single_Switch.STATE.锁闭)
                    {
                        daocha_1_4.state = Single_Switch.STATE.空闲;
                        daocha_1_4.draw();
                    }
                }


            }//if
        }//按钮函数
        #endregion

        #region  当前时间显示
        /// <summary>
        /// 显示当前时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        private void timer1_Tick(object sender, EventArgs e)//实现显示时间功能
        {
            Time.Text = DateTime.Now.ToString();
            
        }
        #endregion

        #region 总人解功能实现
        public void Counter()
        {
            timer2.Tick += timer2_Tick;
           
        }
        
       TimeSpan dtTo = new TimeSpan(0, 0, 20); //设置开始时间
       private void timer2_Tick(object sender, EventArgs e)//实现3分钟倒计时功能
       {
           dtTo = dtTo.Subtract(new TimeSpan(0, 0, 1));
           textBox_ZRJ.Text = dtTo.Hours.ToString() + ":" + dtTo.Minutes.ToString() + ":" + dtTo.Seconds;
           if (dtTo.TotalSeconds < 0.0)//当倒计时完毕
           {
               textBox_ZRJ.Text = " ";
               timer2.Enabled = true;
               //flagJYJ = true;
               routeremove.RouteCancle();
           }


       }
        #endregion


        #region  微机监测模块
        //整体利用类实现

        //通信连接模块

        //采集信息模块：包括信号机采集、轨道电路采集、道岔采集、操作界面个按钮信息采集；（分函数分别采集，返回为String类型）；

        //状态信息整理模块；函数实现，返回String字符串；

        //定义通信协议模块；函数实现返回String类型的字符串

        //信息传输模块，通过线程进行传输；

        //采集时间间隔，一秒采集5次即可，利用时钟实现此功能；
       UdpClient clientSocket_csm;
        private void 微机监测连接ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IPEndPoint udpPoint = new IPEndPoint(IPAddress.Parse("192.168.1.35"), 4505);
            clientSocket_csm = new UdpClient(udpPoint);
            MessageBox.Show("微机监测客户端开启");
        }
        //建立UDP通信，使用时钟加线程的方式实现转发
       private void timer3_Tick(object sender, EventArgs e)
        {
            try
            {
                byte[] data = new byte[1024];
                CSM interlockcsm = new CSM();
                data = Encoding.ASCII.GetBytes(interlockcsm.Informationsummary().ToString());
                IPEndPoint point = new IPEndPoint(IPAddress.Parse("192.168.1.206"), 7788);
                clientSocket_csm.Send(data, data.Length, point);
            }
            catch
            {
            }
       }
     
        #endregion

      

      

    }
}