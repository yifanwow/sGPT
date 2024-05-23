using System;
using System.Windows.Forms;
using DotNetEnv; // 引入DotNetEnv命名空间

namespace MyCSharpProject
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Env.Load(".env.local"); // 加载.env.local文件中的环境变量
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
