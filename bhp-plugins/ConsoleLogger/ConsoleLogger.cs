using System;

namespace Bhp.Plugins
{
    /// <summary>
    /// Console show log
    /// </summary>
    public class ConsoleLogger : Plugin,ILogPlugin
    {
        public new void Log(string source, LogLevel level, string message)
        { 
            string line = $"[{DateTime.UtcNow.TimeOfDay:hh\\:mm\\:ss\\:fff}] [{source}][{level}]{message}";
            Console.WriteLine(line);
        } 
    }
}
