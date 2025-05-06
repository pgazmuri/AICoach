using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AICoach.Services;

namespace AICoach;

public class TrayAppContext : ApplicationContext
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private System.Windows.Forms.Timer activityTimer;
    private string promptFilePath;
    private string suggestion = string.Empty;
    private bool isPaused = false;
    private ToolStripMenuItem pauseMenuItem;
    
    // Services
    private readonly ConfigurationService _configService;
    private readonly ScreenshotService _screenshotService;
    private readonly OpenAIService _openAiService;
    private readonly NotificationService _notificationService;
    private readonly ActivityMonitorService _activityMonitorService;

    public TrayAppContext()
    {
        // Initialize services
        _configService = new ConfigurationService();
        _screenshotService = new ScreenshotService();
        _openAiService = new OpenAIService(_configService, _screenshotService);
        
        // Set up paths
        promptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prompt.txt");

        // Build tray menu with Analyze and Exit
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Analyze...", null, OnAnalyze);
        pauseMenuItem = new ToolStripMenuItem("Pause", null, OnPauseResume);
        trayMenu.Items.Add(pauseMenuItem);
        trayMenu.Items.Add("Exit", null, OnExit);

        trayIcon = new NotifyIcon
        {
            Text = "AI Coach",
            Icon = SystemIcons.Application,
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        
        // Initialize services that need UI components
        _notificationService = new NotificationService(trayIcon);
        _activityMonitorService = new ActivityMonitorService();

        // Set up activity timer
        activityTimer = new System.Windows.Forms.Timer { Interval = 60 * 1000 };
        activityTimer.Tick += ActivityTimer_Tick;
        activityTimer.Start();
    }

    // New menu handler
    private async void OnAnalyze(object? sender, EventArgs e)
    {
        Logger.Instance.Log("Analyze started.");
        try
        {
            var screenshot = _screenshotService.CaptureScreenshot();
            Logger.Instance.Log("Screenshot captured.");
            
            string prompt = ReadPrompt();
            Logger.Instance.Log($"Prompt read: {prompt}, sending to GPT.");
            
            string suggestion = await _openAiService.GetAISuggestionAsync(screenshot, prompt);
            if (!string.IsNullOrEmpty(suggestion) && !suggestion.StartsWith("Error:"))
                _notificationService.ShowAISuggestion(suggestion);
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Analyze failed: {ex.Message}");
        }
    }

    private void ActivityTimer_Tick(object? sender, EventArgs e)
    {
        if (!isPaused && _activityMonitorService.IsUserActive())
        {
            OnAnalyze(null, new EventArgs());
            Logger.Instance.Log("User is active, screenshot taken.");
        }
    }

    private string ReadPrompt()
    {
        var activeWindowTitle = _screenshotService.GetActiveWindowTitle();
        var pre_pend = "";
        if(activeWindowTitle.Length > 0)
            pre_pend = $"The active window in this screenshot is: {activeWindowTitle}. ";
        
        try
        {
            if (File.Exists(promptFilePath))
                return pre_pend + File.ReadAllText(promptFilePath);
        }
        catch { }
        
        return pre_pend + "Analyze this screenshot and suggest if AI could help.";
    }

    private void OnPauseResume(object? sender, EventArgs e)
    {
        isPaused = !isPaused;
        pauseMenuItem.Text = isPaused ? "Resume" : "Pause";
        Logger.Instance.Log(isPaused ? "Paused." : "Resumed.");
    }

    private void OnExit(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        Application.Exit();
    }
}
