using System.Runtime.InteropServices;
using System.Threading;

namespace bhp
{ 
    class Program
    {
        static void Main(string[] args)
        {
            int pid = fork();
            if (pid != 0)
            {
                exit(0);
            }
            //设置“会话组长”，与父进程脱离
            setsid();
            pid = fork();
            if (pid != 0)
            {
                exit(0);
            }

            //已经进入“守护进程”工作状态了!

            //关闭所有打开的文件描述符
            int max = open("/dev/null", 0);
            for (var i = 0; i <= max; i++)
            {
                close(i);
            }

            //重设文件掩模
            umask(0);

            //执行你的程序过程
            DaemonMain(args);
        }


        /// <summary>
        /// Daemon工作状态的主方法
        /// </summary>
        /// <param name="args"></param>
        static void DaemonMain(string[] args)
        {
            //你的工作代码...
            //daemon时，控制台输入输出流已经关闭
            //请不要再用Console.Write/Read等方法

            //阻止daemon进程退出
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        [DllImport("libc", SetLastError = true)]
        static extern int fork();

        [DllImport("libc", SetLastError = true)]
        static extern int setsid();

        [DllImport("libc", SetLastError = true)]
        static extern int umask(int mask);

        [DllImport("libc", SetLastError = true)]
        static extern int open([MarshalAs(UnmanagedType.LPStr)]string pathname, int flags);

        [DllImport("libc", SetLastError = true)]
        static extern int close(int fd);

        [DllImport("libc", SetLastError = true)]
        static extern int exit(int code);
    }    
}
