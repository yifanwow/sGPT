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
            // 设置窗体的背景色为灰黑色
            this.BackColor = Color.FromArgb(30, 30, 30); // 使用 RGB 值表示灰黑色

            // 设置窗体中文字的颜色为白色
            this.ForeColor = Color.White;
            SetupTrayIcon();
            InitializeFolder();
            this.Size = new Size(700, 1230);

            messageLabel = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(780, 20),
                Text = "Press F9 to capture globally."
            };
            this.Controls.Add(messageLabel);

            thumbnailBox = new PictureBox
            {
                Location = new Point(10, 35),
                Size = new Size(80, 45),
                BorderStyle = BorderStyle.Fixed3D,
            };
            this.Controls.Add(thumbnailBox);

            responseWebView = new WebView2
            {
                Location = new Point(10, 100),
                Size = new Size(670, 1070),
                Anchor =
                    AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 50, 0)
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
            await responseWebView.EnsureCoreWebView2Async(null);
            responseWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            responseWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            responseWebView.CoreWebView2.Settings.IsScriptEnabled = true;
            responseWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;

            // 如果有适用的属性设置
            // webView.CoreWebView2.Settings.IsFileAccessFromFileUrlsAllowed = true;
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

                        htmlContent =
                            @"
<html>
<head>
    <meta charset='UTF-8'>
    <link rel='stylesheet' href='file:///F:/Program/Github-Project-Local/sGPT/Project/Assets/atom-one-dark.min.css'>
    <script src='file:///F:/Program/Github-Project-Local/sGPT/Project/Assets/highlight.min.js'></script>
    <style>
        body { font-family: 'Roboto', sans-serif; }
        pre, code { white-space: pre-wrap; word-wrap: break-word; overflow-wrap: break-word; font-family: Consolas, Monaco, 'Courier New', monospace, 'Microsoft YaHei', sans-serif; }
        pre { padding: 10px; background-color: #f5f5f5; border: 1px solid #ddd; border-radius: 4px; }
    </style>
</head>
<body>"
                            + htmlContent
                            + @"<script>
window.onload = function() {
    hljs.highlightAll();
};
</script>
</body>
</html>";

                        string htmlFilePath = SaveHtmlToFile(htmlContent, timeStamp);
                        responseWebView.Invoke(
                            new Action(
                                () =>
                                    responseWebView.CoreWebView2.Navigate($"file:///{htmlFilePath}")
                            )
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

        private string SaveHtmlToFile(string htmlContent, string timeStamp)
        {
            string path = Path.Combine(Application.StartupPath, "html");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fileName = Path.Combine(path, $"{timeStamp}.html");
            File.WriteAllText(fileName, htmlContent);
            return fileName; // Return the full path of the saved HTML file
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
