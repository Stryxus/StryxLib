using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace StryxLib;

/*
 * This Logger takes the load off of the thread which is making the log and prints the result in order asynchronously.
 */
public static class Logger
{
    private static readonly ConcurrentQueue<LogPackage> Queue = new();

    static Logger()
    {
        ClearBuffer();
        // This puts a forever while loop in a seperate thread which will try to dequeue anything occupying the ConcurrentQueue. The while loop will break when the process is terminated.
        Task.Run(() =>
        {
            while (true)
            {
                if (!Queue.IsEmpty && Queue.TryDequeue(out LogPackage result)) PushLog(result);
            }
        });
    }

    private static void PushLog(LogPackage pckg)
    {
        // Determine what formatting should be used while also using standard .NET stdout.
        if (pckg.ClearMode is 0)
        {
            if (pckg.Level is -1) Console.Write(pckg.Message);
            else if (pckg.Level is 0) Console.WriteLine(pckg.Message);
            else if (pckg.Level is 1) Console.WriteLine(pckg.Message);
            else if (pckg.Level is 2) Console.Error.WriteLine(pckg.Message);
            else if (pckg.Level is 3) Debug.WriteLine(pckg.Message);
        }
        else if (pckg.ClearMode is 3) Console.Clear();
        else if (pckg.ClearMode is 2) Console.WriteLine();
        else if (pckg.ClearMode is 1) Console.WriteLine(pckg.Message);
    }

    public static void Log(object msg)
    {
        LogPackage pckg = default;
        pckg.Level = -1;
        pckg.Message = msg is not null ? $"{msg}" : "null";
        PushLog(pckg);
    }

    public static void LogInfo(object msg)
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 0;
        pckg.Message = msg is not null ? $"[INFO] {msg}" : "null";
        PushLog(pckg);
    }

    public static void LogWarn(object msg)
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 1;
        pckg.Message = $"\u001b[33;1m{(msg is not null ? $"[WARN]  {msg}" : "null")}\u001b[0m";
        PushLog(pckg);
    }

    public static void LogError(object msg)
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 2;
        pckg.Message = $"\u001b[31;1m{(msg is not null ? $"[ERR]  {msg}" : "null")}\u001b[0m";
        PushLog(pckg);
    }

#if DEBUG
    public static void LogDebug(object msg)
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 3;
        pckg.Message = msg is not null ? $"[DBG]  {msg}" : "null";
        PushLog(pckg);
    }
#else
    public static void LogDebug(object msg) {}
#endif

    public static void LogException<T>(T e) where T : Exception
    {
        LogPackage pckg = default;
        pckg.PostTime = DateTime.Now;
        pckg.Level = 2;
        pckg.Message = $"[EXCE] Source: {e.Source}\n | Data: {e.Data}\n | Message: {e.Message}\n | StackTrace: {e.StackTrace}";
        PushLog(pckg);
    }

    public static void NewLine(int lines = 1)
    {
        if (lines < 1) lines = 1;
        for (int i = 0; i < lines; i++)
        {
            LogPackage pckg = default;
            pckg.ClearMode = 2;
            PushLog(pckg);
        }
    }

    public static void DivideBuffer()
    {
        StringBuilder b = new();
        for (int i = 0; i < Console.BufferWidth - 1; i++) b.Append('-');
        LogPackage pckg = default;
        pckg.ClearMode = 1;
        pckg.Message = b.ToString();
        PushLog(pckg);
    }

    public static void ClearLine(string? content = null)
    {
        StringBuilder b = new(content is null ? string.Empty : content);
        for (int i = 0; i < Console.BufferWidth - 1; i++) b.Append(' ');
        Console.Write("\r{0}", b.ToString());
    }

    public static void ClearBuffer()
    {
        LogPackage pckg = default;
        pckg.ClearMode = 3;
        PushLog(pckg);
    }

    private struct LogPackage
    {
        internal DateTime PostTime { get; set; }
        internal int ClearMode { get; set; }
        internal int Level { get; set; }
        internal string Message { get; set; }
    }
}