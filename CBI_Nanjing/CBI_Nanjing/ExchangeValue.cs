using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBI_Nanjing
{
   public static  class ExchangeValue
    {
       public static int Snumber=1;//引导接车时用于两个界面的数据交互
       public static int Xnumber = 1;//记录下行的按压次数
       public static bool SorX=true;//区分上下行 下true；下false；

       public static int QGnumber = 0;//区段故障解锁时用于两个界面的数据交互  记录按压的次数
       public static String QG = null; //存标志位
    }
}
