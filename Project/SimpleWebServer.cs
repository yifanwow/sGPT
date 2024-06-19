using System;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;

namespace MyCSharpProject
{
    public class SimpleWebServer
    {
        private readonly HttpListener _listener;
        private string _htmlFilePath;
        private Thread _listenerThread;
        private bool _isRunning;

        public SimpleWebServer(string prefix, string htmlFilePath)
        {
            _listener = new HttpListener();
            _htmlFilePath = htmlFilePath;
            _listener.Prefixes.Add(prefix);
        }

        public void Start()
        {
            try
            {
                _isRunning = true;
                _listener.Start();
                _listenerThread = new Thread(Listen);
                _listenerThread.Start();
                ConsoleManager.WriteLine("Web server started successfully.");
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Failed to start web server: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            _listenerThread.Join();
        }

        public void UpdateHtmlFilePath(string newHtmlFilePath)
        {
            _htmlFilePath = newHtmlFilePath;
        }

        private void Listen()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ProcessRequest(context);
                }
                catch (HttpListenerException ex)
                {
                    if (!_isRunning)
                        return;
                    ConsoleManager.WriteLine($"Listener exception: {ex.Message}");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                if (File.Exists(_htmlFilePath))
                {
                    string responseString = File.ReadAllText(_htmlFilePath);
                    responseString = responseString.Replace(
                        "file:///F:/Program/Github-Project-Local/sGPT/Project/Assets/atom-one-dark.min.css",
                        "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.3.1/styles/atom-one-dark.min.css"
                    );
                    responseString = responseString.Replace(
                        "file:///F:/Program/Github-Project-Local/sGPT/Project/Assets/highlight.min.js",
                        "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.3.1/highlight.min.js"
                    );
                    responseString = responseString.Replace("font-size: 19px;", "font-size: 23px;"); //修改字体大小
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    context.Response.ContentType = "text/html";
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    string responseString = "<html><body><h1>File not found</h1></body></html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    context.Response.StatusCode = 404;
                    context.Response.ContentType = "text/html";
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                context.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error processing request: {ex.Message}");
            }
        }
    }
}
