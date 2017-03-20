using System;
using System.IO;

namespace SniperPlugin
{
    public class Logger
    {
        public static bool Enabled { get; set; }

        public static void Write(string text)
        {
            if (!Enabled) return;
            text = DateTime.Now.ToString("dd-MM-yy HH:mm:ss") + " | " + text;
            using (var writer = new StreamWriter(@"Plugins\SniperPluginLog.txt", true))
            {
                writer.WriteLine(text);
            }
        }
    }
}
