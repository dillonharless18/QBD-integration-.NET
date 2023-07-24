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
                var logEntry = CreateLogEntry(logMessage);
                w.WriteLine(logEntry);
                Console.WriteLine(logEntry);  // Also write to the console
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions related to writing to the log file here
            // It may be best to simply output these to the console or debug output
            Console.Write("Failed to write to log: " + ex.Message);
        }
    }

    private static string CreateLogEntry(string logMessage)
    {
        return "\r\nLog Entry : " +
               "\n" + DateTime.Now.ToLongTimeString() + " " + DateTime.Now.ToLongDateString() +
               "\n  :" + logMessage +
               "\n-------------------------------";
    }
}
