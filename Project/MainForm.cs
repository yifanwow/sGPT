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
        Label consoleLabel;
        int count = 0;
        private SimpleWebServer webServer;
        private Button toggleSliderButton;
        private TrackBar opacitySlider;

        public MainForm()
        {
            ConsoleManager.WriteLine("Starting sGPT...");
            this.Text = "sGPT";

            // 设置窗体的背景色为灰黑色
            this.BackColor = Color.FromArgb(90, 90, 90); // 使用 RGB 值表示灰黑色

            // 设置窗体中文字的颜色为白色
            this.ForeColor = Color.White;
            SetupTrayIcon();
            InitializeFolder();
            this.Size = new Size(700, 1130);

            // 使用TableLayoutPanel来布局
            TableLayoutPanel layoutPanel = new TableLayoutPanel
            {
                RowCount = 2,
                ColumnCount = 2,
                Dock = DockStyle.Top,
                Size = new Size(700, 50),
                AutoSize = true,
                BackColor = Color.FromArgb(70, 70, 70)
            };
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            messageLabel = new Label
            {
                Text = "Press F9 to capture globally.",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layoutPanel.Controls.Add(messageLabel, 0, 0);

            thumbnailBox = new PictureBox
            {
                Size = new Size(80, 45),
                BorderStyle = BorderStyle.Fixed3D,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            layoutPanel.Controls.Add(thumbnailBox, 0, 1);

            consoleLabel = new Label
            {
                Text = "Console Output:",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layoutPanel.Controls.Add(consoleLabel, 1, 0);
            layoutPanel.SetRowSpan(consoleLabel, 3); // 让consoleLabel占据右侧两行

            this.Controls.Add(layoutPanel);

            responseWebView = new WebView2
            {
                Location = new Point(10, 90),
                Size = new Size(670, 1000),
                Anchor =
                    AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 50, 0),
                DefaultBackgroundColor = Color.FromArgb(30, 30, 30)
            };
            this.Controls.Add(responseWebView);

            toggleSliderButton = new Button
            {
                Text = "Toggle Opacity Slider",
                Dock = DockStyle.Top,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            toggleSliderButton.Click += ToggleSliderButton_Click;
            this.Controls.Add(toggleSliderButton);

            opacitySlider = new TrackBar
            {
                Minimum = 17,
                Maximum = 100,
                Value = 100,
                TickFrequency = 10,
                Dock = DockStyle.Top,
                Visible = false
            };
            opacitySlider.Scroll += OpacitySlider_Scroll;
            this.Controls.Add(opacitySlider);

            ConsoleManager.MessagesUpdated += UpdateConsoleLabel;

            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHookKeyDown;

            this.Resize += MainForm_Resize;
            this.FormClosing += MainForm_FormClosing;
            trayIcon.DoubleClick += TrayIcon_DoubleClick;

            this.ShowInTaskbar = false;

            InitializeAsync();
            StartWebServer();
        }

        private void ToggleSliderButton_Click(object sender, EventArgs e)
        {
            opacitySlider.Visible = !opacitySlider.Visible;
        }

        private void OpacitySlider_Scroll(object sender, EventArgs e)
        {
            this.Opacity = opacitySlider.Value / 100.0;
        }

        private void UpdateConsoleLabel()
        {
            if (consoleLabel.InvokeRequired)
            {
                consoleLabel.Invoke(new Action(UpdateConsoleLabel));
            }
            else
            {
                consoleLabel.Text = ConsoleManager.GetLatestMessages();
            }
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
            if (e.KeyCode == Keys.F9 || e.KeyCode == Keys.F1 || e.KeyCode == Keys.F3)
            {
                ConsoleManager.WriteLine($"捕获到{e.KeyCode}...");
                count++;
                string fileName = null;
                string clipboardContent = null;

                if (e.KeyCode == Keys.F9 || e.KeyCode == Keys.F1)
                {
                    fileName = CaptureScreenshot();
                    DisplayThumbnail(fileName);
                }
                else if (e.KeyCode == Keys.F3)
                {
                    clipboardContent = GetClipboardText();
                }

                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                messageLabel.Text = $"Successfully captured, Count: {count}, Time: {timeStamp}";

                ConsoleManager.WriteLine("发送api请求...");

                string promptEnvVar =
                    e.KeyCode == Keys.F9
                        ? "OPENAI_PROMPT"
                        : (e.KeyCode == Keys.F1 ? "OPENAI_PROMPTBOTH" : "OPENAI_PROMPT_CLIPBOARD");
                string prompt = Environment.GetEnvironmentVariable(promptEnvVar);

                if (e.KeyCode == Keys.F3)
                {
                    ApiHandler
                        .CallOpenAiApiForClipboard(prompt, clipboardContent)
                        .ContinueWith(task =>
                        {
                            var content = ApiResponseHandler.ExtractContentFromResponse(
                                task.Result
                            );
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
        body { font-family: 'Roboto', sans-serif;  background-color: #303030;color: #e7e7e7;font-size: 17px;}
        pre, code { white-space: pre-wrap; word-wrap: break-word; overflow-wrap: break-word; font-family: Consolas, Monaco, 'Courier New', monospace; 'Microsoft YaHei', sans-serif; }
        pre { padding: 7px; background-color: #888888; border: 1px solid #6e6e6e; border-radius: 3px; }
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

                            ConsoleManager.WriteLine("尝试保存文件到本地。");
                            string htmlFilePath = SaveHtmlToFile(htmlContent, timeStamp);
                            webServer.UpdateHtmlFilePath(htmlFilePath); // 更新HTML文件路径
                            responseWebView.Invoke(
                                new Action(
                                    () =>
                                        responseWebView.CoreWebView2.Navigate(
                                            $"file:///{htmlFilePath}"
                                        )
                                )
                            );
                        });
                }
                else
                {
                    ApiHandler
                        .CallOpenAiApi(fileName, prompt)
                        .ContinueWith(task =>
                        {
                            var content = ApiResponseHandler.ExtractContentFromResponse(
                                task.Result
                            );
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
        body { font-family: 'Roboto', sans-serif;  background-color: #303030;color: #e7e7e7;font-size: 17px;}
        pre, code { white-space: pre-wrap; word-wrap: break-word; overflow-wrap: break-word; font-family: Consolas, Monaco, 'Courier New', monospace; 'Microsoft YaHei', sans-serif; }
        pre { padding: 7px; background-color: #888888; border: 1px solid #6e6e6e; border-radius: 3px; }
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

                            ConsoleManager.WriteLine("尝试保存文件到本地。");
                            string htmlFilePath = SaveHtmlToFile(htmlContent, timeStamp);
                            webServer.UpdateHtmlFilePath(htmlFilePath); // 更新HTML文件路径
                            responseWebView.Invoke(
                                new Action(
                                    () =>
                                        responseWebView.CoreWebView2.Navigate(
                                            $"file:///{htmlFilePath}"
                                        )
                                )
                            );
                        });
                }

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

        private string GetClipboardText()
        {
            string clipboardText = string.Empty;
            if (Clipboard.ContainsText())
            {
                clipboardText = Clipboard.GetText();
            }
            return clipboardText;
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
            try
            {
                webServer.Stop();
                m_GlobalHook.Dispose();
                trayIcon.Dispose();
                Application.Exit();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine("Error on exit: " + ex.Message);
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                webServer.Stop();
                m_GlobalHook.Dispose();
                trayIcon.Dispose();
                Application.Exit();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine("Error on exit: " + ex.Message);
            }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowWindow(this.Handle, SW_SHOWNA);
            this.WindowState = FormWindowState.Normal;
        }

        private void StartWebServer()
        {
            string initialHtmlFilePath = Path.Combine(
                Application.StartupPath,
                "html",
                "initial.html"
            );

            // Ensure the initial.html file exists to prevent errors
            if (!File.Exists(initialHtmlFilePath))
            {
                File.WriteAllText(
                    initialHtmlFilePath,
                    "<html><body><h1>Initial File</h1></body></html>"
                );
            }

            try
            {
                webServer = new SimpleWebServer("http://+:8080/", initialHtmlFilePath);
                webServer.Start();
                ConsoleManager.WriteLine("Web server started on http://localhost:8080/");
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine("Failed to start web server: " + ex.Message);
            }
            //192.168.1.247
        }
    }
}
