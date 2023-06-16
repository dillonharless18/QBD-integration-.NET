using System;
using System.IO;

public static class Logger
{
    public static void Log(string logMessage)
    {
        try
        {
            using (StreamWriter w = File.AppendText("oneXerpLog.txt"))
            {
                w.WriteLine("\r\nLog Entry : ");
                w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                w.WriteLine($"  :{logMessage}");
                w.WriteLine("-------------------------------");
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions related to writing to the log file here
            // It may be best to simply output these to the console or debug output
            Console.Write("Failed to write to log: " + ex.Message);
        }
    }
}
