using System;
using System.IO;

namespace AICoach;

public class Logger
{
    private static readonly Lazy<Logger> _instance = new(() => new Logger());
    private readonly string _logFilePath = "";

    private Logger()
    {
        string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
		Console.WriteLine($"Log file path: {_logFilePath}");
    }

    public static Logger Instance => _instance.Value;

    public void Log(string message)
    {
        File.AppendAllText(_logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
    }
}