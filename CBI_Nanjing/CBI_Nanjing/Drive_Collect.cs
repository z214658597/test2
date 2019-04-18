using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBI_Nanjing
{
    class Drive_Collect
    {
        public static Int32[,] Collect_Data_CH365_IO = new Int32[2, 4];
        public static Int32[,,] Collect_Split_Data_CH365_IO = new Int32[2, 4, 8];  //2块采集板，每块4*8=32位
        public static Int32[,] Drive_Data_CH365_IO = new Int32[4, 4];
        public static Int32[,,] Drive_Split_Data_CH365_IO = new Int32[4, 4, 8];  //4块驱动板，每块4*8=32位

        //南京南站，共12个站内信号机，区间不设信号机，所有信号机名称
        public static string[] Nanjing_name_CH365_Signal = new string[12] {
            "NJN_X","NJN_XF","NJN_S1","NJN_S2","NJN_S3","NJN_S4",
            "NJN_S","NJN_SF","NJN_X1","NJN_X2","NJN_X3","NJN_X4" };

        //定义三维数组 第一维代表12个信号机 第二维为灯位信息（U ,L, H, 2U, B） 第三维为每个灯在板卡中的具体位置，
        //每一块CH365芯片同时只能传输8位数据，而PCI是32位的，因此分为4组数据；比如：{0，1，5}表示0号板卡的第1组
        //数据的第5位；而{3,3,7}即代表表示3号板卡的第3组数据的第7位，而一个站只有两块板卡，因此可以理解为悬空位置
        //对应的板卡位置
        public static Int32[,,] Nanjing_location_CH365_Signal = new Int32[12, 5, 3] {       
        //  U       L       H      2U       B       分别是对应的信号机不同颜色的驱动位置
        {{0,0,0},{0,0,2},{0,0,4},{0,0,6},{0,1,0}}, //NJN_X
        {{0,1,2},{0,1,4},{0,1,6},{0,2,0},{0,2,2}}, //NJN_XF
        {{0,2,6},{0,2,4},{0,3,0},{3,3,7},{3,3,7}}, //NJN_S1
        {{0,2,3},{0,3,2},{0,2,5},{3,3,7},{3,3,7}}, //NJN_S2
        {{0,3,1},{0,2,7},{0,3,3},{3,3,7},{3,3,7}}, //NJN_S3
        {{0,0,3},{0,0,1},{0,0,5},{3,3,7},{3,3,7}}, //NJN_S4

        {{1,0,0},{1,0,2},{1,0,4},{1,0,6},{1,1,0}}, //NJN_S
        {{1,1,2},{1,1,4},{1,1,6},{1,2,0},{1,2,2}}, //NJN_SF
        {{1,2,6},{1,2,4},{1,3,0},{3,3,7},{3,3,7}}, //NJN_X1
        {{1,2,3},{1,3,2},{1,2,5},{3,3,7},{3,3,7}}, //NJN_X2      
        {{1,3,1},{1,2,7},{1,3,3},{3,3,7},{3,3,7}}, //NJN_X3
        {{1,0,3},{1,0,1},{1,0,5},{3,3,7},{3,3,7}}, //NJN_X4                               
        };

        //南京南站所有区段名称，包括区间和站内所有需要采集的无岔区段和道轨区段
        public static string[] Nanjing_name_CH365_Track = new string[20] {
          "daocha_1_1",
          "daocha_1_3",
          "daocha_1_2",
          "daocha_1_4",
          "Track_G1",
          "Track_G2",
          "Track_G3",
          "Track_G4",
          "dangui_8",
          "dangui_10",
          "dangui_12",
          "dangui_7",
          "dangui_9",
          "dangui_11",
          "dangui_1",
          "dangui_3",
          "dangui_5",
          "dangui_6",
          "dangui_4",
          "dangui_2"
        };
        //对应的板卡位置
        public static Int32[,] Nanjing_location_CH365_Track = new Int32[20, 3] {
            {0,1,5},  //1DG
            {0,1,7},  //3DG
            {0,3,4},  //2DG
            {0,3,6},  //4DG
            {0,2,4},  //G1
            {0,2,6},  //G2
            {0,3,0},  //G3
            {0,3,2},  //G4
            {1,0,5},  //S3JG对应dangui_8
            {1,0,3},  //S2JG对应dangui_10
            {1,0,1},  //S1JG对应dangui_12


            {1,0,7},  //X1LQ对应dangui_7
            {1,1,1},  //X2LQ对应dangui_9
            {1,1,3},  //X3LQ对应dangui_11

            {0,0,1},  //X1JG对应dangui_1
            {0,0,3},  //X2JG对应dangui_3
            {0,0,5},  //X3JG对应dangui_5
            {0,0,7},  //S1LQ对应dangui_6 
            {0,1,1},  //S2LQ对应dangui_4
            {0,1,3}   //S3LQ对应dangui_2                                              
        };

        //南京南站道岔名称，包括所有道岔
        public static string[] Nanjing_name_CH365_Switch = new string[4] {
            "daocha_1_1",
            "daocha_1_3",
            "daocha_1_2",
            "daocha_1_4",
        };

        //对应的板卡位置；暂时采集定位  
        public static Int32[,,] Nanjing_location_CH365_Switch = new Int32[4, 3, 3] {
           {{0,3,4},{0,3,5},{0,0,0}}, //分别为道岔1的定位、反位驱动板卡位置以及采集该道岔定位的板卡位置 
           {{0,3,6},{0,3,7},{0,0,4}}, //分别为道岔3的定位、反位驱动板卡位置以及采集该道岔定位的板卡位置
           {{1,3,4},{1,3,5},{0,1,4}}, //分别为道岔2的定位、反位驱动板卡位置以及采集该道岔定位的板卡位置
           {{1,3,7},{1,3,6},{0,2,1}}  //分别为道岔4的定位、反位驱动板卡位置以及采集该道岔定位的板卡位置           
        };
    }
}
