using System.Windows.Forms;
using Gma.System.MouseKeyHook;

namespace MyCSharpProject
{
    public class GlobalHookHandler
    {
        private IKeyboardMouseEvents m_GlobalHook;

        public void Start()
        {
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHookKeyDown;
        }

        public void Stop()
        {
            m_GlobalHook.KeyDown -= GlobalHookKeyDown;
            m_GlobalHook.Dispose();
        }

        private void GlobalHookKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F9)
            {
                // 处理F9键按下事件
            }
        }
    }
}
