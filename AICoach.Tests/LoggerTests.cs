using System;
using System.IO;
using Xunit;

namespace AICoach.Tests
{
    public class LoggerTests
    {
        [Fact]
        public void Log_WritesMessageToFile()
        {
            // Arrange
            string testMessage = "Test log message";
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            string logFilePath = Path.Combine(logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.txt");

            // Ensure the log file does not exist before the test
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            // Act
            Logger.Instance.Log(testMessage);

            // Assert
            Assert.True(File.Exists(logFilePath), "Log file was not created.");
            string logContent = File.ReadAllText(logFilePath);
            Assert.Contains(testMessage, logContent);
        }
    }
}