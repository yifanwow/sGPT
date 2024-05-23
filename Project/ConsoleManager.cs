using System;
using System.Collections.Generic;

namespace MyCSharpProject
{
    public static class ConsoleManager
    {
        private static List<string> _messages = new List<string>();
        public static event Action MessagesUpdated;

        public static void WriteLine(string message)
        {
            if (_messages.Count >= 2)
            {
                _messages.RemoveAt(0);
            }
            _messages.Add(message);
            MessagesUpdated?.Invoke();
        }

        public static string GetLatestMessages()
        {
            return string.Join(Environment.NewLine, _messages);
        }
    }
}
