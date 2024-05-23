using Newtonsoft.Json;

namespace MyCSharpProject
{
    public static class ApiResponseHandler
    {
        public static string ExtractContentFromResponse(string jsonResponse)
        {
            var responseObject = JsonConvert.DeserializeObject<OpenAiResponse>(jsonResponse);
            return responseObject.choices[0].message.content;
        }
    }

    public class OpenAiResponse
    {
        public string id { get; set; }
        public string _object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }
        public Usage usage { get; set; }
        public string system_fingerprint { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
}
