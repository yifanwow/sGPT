using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MyCSharpProject
{
    public static class ApiHandler
    {
        public static async Task<string> CallOpenAiApi(string imagePath)
        {
            ConsoleManager.WriteLine("读取环境变量...");
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            ConsoleManager.WriteLine($"API KEY={apiKey}"); // 从环境变量中读取API key
            var promptText = Environment.GetEnvironmentVariable("OPENAI_PROMPT");

            var uri = "https://api.openai.com/v1/chat/completions"; // 使用正确的API端点
            ConsoleManager.WriteLine($"API 端点={uri}");

            using (var client = new HttpClient())
            using (var formData = new MultipartFormDataContent())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey
                );

                var messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = promptText },
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(imagePath))}"
                                }
                            }
                        }
                    }
                };

                var data = new
                {
                    model = "gpt-4o", // 使用GPT-4 Turbo模型
                    messages = messages,
                    //max_tokens = 300 // 根据需要调整生成文本的长度
                };

                var jsonContent = new StringContent(
                    JsonConvert.SerializeObject(data),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(uri, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return $"Error: {response.StatusCode}";
                }
            }
        }
    }
}
