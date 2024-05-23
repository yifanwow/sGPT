using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;

namespace MyCSharpProject
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_SHOWNA = 8;
        private IKeyboardMouseEvents m_GlobalHook;
        Label messageLabel;
        PictureBox thumbnailBox;
        WebBrowser responseWebBrowser; // 使用WebBrowser控件
        NotifyIcon trayIcon;
        ContextMenuStrip trayMenu;
        int count = 0;

        public MainForm()
        {
            this.Text = "sGPT";
            SetupTrayIcon();
            InitializeFolder();
            this.Size = new Size(800, 600); // 调整窗口大小以适应WebBrowser控件

            messageLabel = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(780, 20),
                Text = "Press F9 to capture globally."
            };
            this.Controls.Add(messageLabel);

            thumbnailBox = new PictureBox
            {
                Location = new Point(10, 40),
                Size = new Size(320, 180),
                BorderStyle = BorderStyle.Fixed3D
            };
            this.Controls.Add(thumbnailBox);

            responseWebBrowser = new WebBrowser
            {
                Location = new Point(10, 230),
                Size = new Size(780, 1350)
            };
            this.Controls.Add(responseWebBrowser);

            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHookKeyDown;

            this.Resize += MainForm_Resize;
            trayIcon.DoubleClick += TrayIcon_DoubleClick;

            this.ShowInTaskbar = false;
        }

        private void GlobalHookKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F9)
            {
                count++;
                string fileName = CaptureScreenshot();
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                messageLabel.Text =
                    $"Successfully captured the main screen, Count: {count}, Time: {timeStamp}";
                DisplayThumbnail(fileName);

                ApiHandler
                    .CallOpenAiApi(fileName)
                    .ContinueWith(task =>
                    {
                        var content = ApiResponseHandler.ExtractContentFromResponse(task.Result);
                        var htmlContent = Markdig.Markdown.ToHtml(content);
                        responseWebBrowser.Invoke(
                            new Action(() => responseWebBrowser.DocumentText = htmlContent)
                        );
                    });

                e.Handled = true;
            }
        }

        private void InitializeFolder()
        {
            string path = Path.Combine(Application.StartupPath, "screenshot");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private string CaptureScreenshot()
        {
            string fileName = Path.Combine(
                Application.StartupPath,
                "screenshot",
                $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.png"
            );
            using (
                var bmp = new Bitmap(
                    Screen.PrimaryScreen.Bounds.Width,
                    Screen.PrimaryScreen.Bounds.Height
                )
            )
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(
                        Screen.PrimaryScreen.Bounds.Location,
                        Point.Empty,
                        Screen.PrimaryScreen.Bounds.Size
                    );
                }
                bmp.Save(fileName);
            }
            return fileName;
        }

        private void DisplayThumbnail(string filePath)
        {
            Image img = Image.FromFile(filePath);
            Image thumb = img.GetThumbnailImage(320, 180, () => false, IntPtr.Zero);
            thumbnailBox.Image = thumb;
            img.Dispose();
        }

        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Exit", null, OnExit);

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowWindow(this.Handle, SW_SHOWNA);
            this.WindowState = FormWindowState.Normal;
        }
    }
}
