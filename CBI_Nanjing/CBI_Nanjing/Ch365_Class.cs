using System;
using System.Runtime.InteropServices;

namespace CBI_Nanjing
{
    class Ch365_Class
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct _CH365_IO_REG          //定义一个和VC中一样的结构体，把这个结构体传入到dll函数中，完成参数的封送。
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0xf0)]//字符串的最大长度
            public int mCh365IoPort;

            public Int16 mCh365MemAddr;

            public char mCh365MenAddrL;
            public char mCh365MenAddrH;

            public char mCh365IoResv2;
            public char mCh365MemData;
            public char mCh365I2cData;
            public char mCh365I2cCtrl;
            public char mCh365I2cAddr;
            public char mCh365I2cDev;
            public char mCh365IoCtrl;
            public char mCh365IoBuf;
            public char mCh365Speed;
            public char mCh365IoResv3;
            public char mCh365IoTime;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 3)]
            public char mCh365IoResv4;
        }

        /**********CH365的DLL文件原代码**************************/
        //    typedef	struct	_CH365_IO_REG {					// CH365芯片的I/O空间
        //UCHAR			mCh365IoPort[0xf0];			// 00H-EFH,共240字节为标准的I/O端口
        //union	{									// 以字或者以字节为单位进行存取
        //    USHORT		mCh365MemAddr;				// F0H 存储器接口: A15-A0地址设定寄存器
        //struct	{								// 以字节为单位进行存取
        //    UCHAR	mCh365MemAddrL;				// F0H 存储器接口: A7-A0地址设定寄存器
        //    UCHAR	mCh365MemAddrH;				// F1H 存储器接口: A15-A8地址设定寄存器
        //};
        //UCHAR			mCh365IoResv2;				// F2H
        //UCHAR			mCh365MemData;				// F3H 存储器接口: 存储器数据存取寄存器
        //UCHAR			mCh365I2cData;				// F4H I2C串行接口: I2C数据存取寄存器
        //UCHAR			mCh365I2cCtrl;				// F5H I2C串行接口: I2C控制和状态寄存器
        //UCHAR			mCh365I2cAddr;				// F6H I2C串行接口: I2C地址设定寄存器
        //UCHAR			mCh365I2cDev;				// F7H I2C串行接口: I2C设备地址和命令寄存器
        //UCHAR			mCh365IoCtrl;				// F8H 芯片控制寄存器,高5位只读
        //UCHAR			mCh365IoBuf;				// F9H 本地数据输入缓存寄存器
        //UCHAR			mCh365Speed;				// FAH 芯片速度控制寄存器
        //UCHAR			mCh365IoResv3;				// FBH
        //UCHAR			mCh365IoTime;				// FCH 硬件循环计数寄存器
        //UCHAR			mCh365IoResv4[3];			// FDH

        /*************************************/

        //EntryPoint 入口点名称
        //ExactSpelling 是否必须与指示的入口点拼写完全一致，默认false
        //CallingConvention 入口点调用约定

        [DllImport("CH365DLL.dll", EntryPoint = "CH365mOpenDevice", ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CH365mOpenDevice(int a, bool b, bool c);

        [DllImport("CH365DLL.dll", EntryPoint = "CH365mWriteIoByte", ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern void CH365mWriteIoByte(Int32 index, Int32 iaddr, int idword);

        [DllImport("CH365DLL.dll", EntryPoint = "CH365mReadIoByte", ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern void CH365mReadIoByte([In, Out] int a, int mCh365IoPort, Int32[] idword);

        [DllImport("CH365DLL.dll", EntryPoint = "CH365mCloseDevice", ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern void CH365mCloseDevice(Int32 index);

        /**********CH365DLL.H文件原代码**************************/

        //HANDLE	WINAPI	CH365OpenDevice(  // 打开CH365设备,返回句柄,出错则无效
        //          BOOL			iEnableMemory,  // 是否需要支持存储器
        //          BOOL			iEnableInterrupt );  // 是否需要支持中断

        //VOID	WINAPI	CH365CloseDevice( );  // 关闭CH365设备

        //BOOL	WINAPI	CH365WriteIoByte(  // 向I/O端口写入一个字节
        //      PVOID			iAddr,  // 指定I/O端口的地址
        //      UCHAR			iByte );  // 待写入的字节数据

        //BOOL	WINAPI	CH365ReadIoByte(  // 从I/O端口读取一个字节
        //      PVOID			iAddr,  // 指定I/O端口的地址
        //      PUCHAR			oByte );  // 指向一个字节单元,用于保存读取的字节数据

        /**********CH365的DLL文件原代码**************************/
    }
}
