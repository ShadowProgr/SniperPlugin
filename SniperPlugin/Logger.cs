using System;
using System.IO;

namespace SniperPlugin
{
    public class Logger
    {
        public enum Type
        {
            Info,
            Queue,
            ParseThread,
            SnipeThread,
            RequestThread
        }

        public static bool Enabled { get; set; }

        public static void Write(string text, Type type)
        {
            if (!Enabled) return;
            text = $"{DateTime.Now.ToString($"dd-MM-yy HH:mm:ss"),-17} | {type.ToString(),15} | " + text;
            using (var writer = new StreamWriter(@"Plugins\SniperPlugin\Log.txt", true))
            {
                writer.WriteLine(text);
            }
        }
    }
}
