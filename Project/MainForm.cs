using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using Markdig;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

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
        WebView2 responseWebView;
        NotifyIcon trayIcon;
        ContextMenuStrip trayMenu;
        int count = 0;

        public MainForm()
        {
            this.Text = "sGPT";
            SetupTrayIcon();
            InitializeFolder();
            this.Size = new Size(800, 600);

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

            responseWebView = new WebView2
            {
                Location = new Point(10, 230),
                Size = new Size(780, 350),
                Anchor =
                    AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(responseWebView);

            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHookKeyDown;

            this.Resize += MainForm_Resize;
            trayIcon.DoubleClick += TrayIcon_DoubleClick;

            this.ShowInTaskbar = false;

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            var userDataFolder = Path.Combine(Application.StartupPath, "WebView2Data");
            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            // 确保 WebView2 控件初始化完毕
            await responseWebView.EnsureCoreWebView2Async(environment);

            // 配置 WebView2 设置
            responseWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            responseWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            responseWebView.CoreWebView2.Settings.IsScriptEnabled = true;
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
                        var pipeline = new MarkdownPipelineBuilder()
                            .UseAdvancedExtensions()
                            .Build();
                        var htmlContent = Markdown.ToHtml(content, pipeline);

                        // 使用Prism.js进行代码高亮
                        htmlContent =
                            @"
    <html>
    <head>
        <meta charset='UTF-8'>
        <link rel='stylesheet' href='F:\Program\Github-Project-Local\sGPT\Project\Assets\atom-one-dark.min.css'>
        <script src='F:\Program\Github-Project-Local\sGPT\Project\Assets\highlight.min.js'></script>

        <style>
            body { font-family: 'Roboto', sans-serif; }
            pre, code { white-space: pre-wrap; word-wrap: break-word; overflow-wrap: break-word; font-family: Consolas, Monaco, 'Courier New', monospace, 'Microsoft YaHei', sans-serif; }
            pre { padding: 10px; background-color: #f5f5f5; border: 1px solid #ddd; border-radius: 4px; }
        </style>
    </head>
    <body>"
                            + htmlContent
                            + @"<script>hljs.highlightAll();</script>
    </body>
    </html>";

                        // 保存HTML内容到本地文件
                        SaveHtmlToFile(htmlContent, timeStamp);

                        responseWebView.Invoke(
                            new Action(() => responseWebView.NavigateToString(htmlContent))
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

        private void SaveHtmlToFile(string htmlContent, string timeStamp)
        {
            string path = Path.Combine(Application.StartupPath, "html");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fileName = Path.Combine(path, $"{timeStamp}.html");
            File.WriteAllText(fileName, htmlContent);
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
