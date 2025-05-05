using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using OpenAI;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Transactions;

namespace AICoach;

public class TrayAppContext : ApplicationContext
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private System.Windows.Forms.Timer activityTimer;
    private int screenshotIntervalMinutes = 3; // configurable
    private string promptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prompt.txt");
    private readonly OpenAI.Chat.ChatClient openAiClient;

    // Structure for GetLastInputInfo
    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    // Import GetLastInputInfo function
    [DllImport("user32.dll")]
    static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    public TrayAppContext()
    {
        // Load OpenAI API key from configuration
        var config = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText("appsettings.json"));
        var openAiApiKey = config?["OpenAI"]?["ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing.");

        // Initialize OpenAI client - Use a model that supports vision
        openAiClient = new OpenAI.Chat.ChatClient("gpt-4.1-mini", openAiApiKey);


        // Build tray menu with Analyze and Exit
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Analyze...", null, OnAnalyze);
        trayMenu.Items.Add("Exit", null, OnExit);

        trayIcon = new NotifyIcon
        {
            Text = "AI Coach",
            Icon = SystemIcons.Application,
            ContextMenuStrip = trayMenu,
            Visible = true
        };

        // Timer is no longer started automatically
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
            var screenshot = CaptureScreenshot();
            Logger.Instance.Log("Screenshot captured.");
            string prompt = ReadPrompt();
            Logger.Instance.Log($"Prompt read: {prompt}, sending to GPT.");
            string suggestion = await SendToGPTAsync(screenshot, prompt);
            if (!string.IsNullOrEmpty(suggestion) && !suggestion.StartsWith("Error:"))
                
                ShowAISuggestion(suggestion);
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Analyze failed: {ex.Message}");
        }
    }

    private void ActivityTimer_Tick(object? sender, EventArgs e)
    {
        if (IsUserActive())
        {
            OnAnalyze(null, new EventArgs());
			Logger.Instance.Log("User is active, screenshot taken.");
        }
    }

    private bool IsUserActive()
    {
        LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

        if (!GetLastInputInfo(ref lastInputInfo))
        {
            // Could not get last input info, assume inactive or handle error
            Logger.Instance.Log("Error getting last input info.");
            return false;
        }

        // Get the current tick count
        uint currentTickCount = (uint)Environment.TickCount;

        // Calculate idle time in milliseconds
        uint idleTime = currentTickCount - lastInputInfo.dwTime;

        // Consider user active if idle for less than 1 minute (60000 ms)
        return idleTime < 60000;
    }

    private Bitmap CaptureScreenshot()
    {
        Screen? primaryScreen = Screen.PrimaryScreen;
        if (primaryScreen == null)
        {
            throw new InvalidOperationException("No primary screen detected.");
        }
        Rectangle bounds = primaryScreen.Bounds;
        Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }
        return bitmap;
    }

    private string ReadPrompt()
    {
		var activeWindowTitle = GetActiveWindowTitle();
		var pre_pend = "";
		if(activeWindowTitle.Length > 0)
			pre_pend = $"The active window in this screenshot is: {activeWindowTitle}. ";
        try
        {
            if (File.Exists(promptFilePath))
                return pre_pend + File.ReadAllText(promptFilePath);
        }
        catch { }
        return pre_pend +  "Analyze this screenshot and suggest if AI could help.";
    }

    private async Task<string> SendToGPTAsync(Bitmap screenshot, string prompt)
    {
        try
        {
            // 1. encode screenshot as PNG
            BinaryData imageBytes;
            using (var ms = new MemoryStream())
            {
                screenshot.Save(ms, ImageFormat.Png);
                imageBytes = new BinaryData(ms.ToArray());
            }

            // 2. build chat messages
            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage("You are an AI assistant analyzing screenshots to provide suggestions about how AI could be used to help perform the task shown in the screenshot."),
                ChatMessage.CreateUserMessage(new ChatMessageContentPart[] {
                    ChatMessageContentPart.CreateTextPart(prompt),
                    ChatMessageContentPart.CreateImagePart(imageBytes, "image/png")
                })
            };

            // 3. send to OpenAI via the SDK
            var response = await openAiClient.CompleteChatAsync(messages);
            if (response == null)
                return "No response from OpenAI.";

            return response.Value.Content.Last().Text ?? "No content in response.";
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Exception during OpenAI call: {ex}");
            return $"Error: {ex.Message}";
        }
    }

    private void ShowAISuggestion(string suggestion)
    {
		Logger.Instance.Log($"AI Suggestion: {suggestion}");
		if(suggestion == "No Suggestion"){
			return;
		}

        trayIcon.BalloonTipTitle = "AI Coach";
        trayIcon.BalloonTipText = suggestion;
        trayIcon.BalloonTipIcon = ToolTipIcon.Info;
		trayIcon.BalloonTipClicked -= (s, e) => { }; // Unsubscribe previous event handlers to avoid duplicates
		trayIcon.BalloonTipClicked += (s, e) =>
		{
			MessageBox.Show(suggestion, "AI Coach Suggestion", MessageBoxButtons.OK, MessageBoxIcon.Information);
		};
        trayIcon.ShowBalloonTip(5000); // Show for 5 seconds
    }

	//function to determine the name of the active window
	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr GetForegroundWindow();
	[DllImport("user32.dll", SetLastError = true)]
	private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
	private static string GetActiveWindowTitle()
	{
		IntPtr handle = GetForegroundWindow();
		if (handle == IntPtr.Zero)
		{
			return string.Empty;
		}
		StringBuilder windowTitle = new StringBuilder(256);
		// Get the window title
		int length = GetWindowText(handle, windowTitle, windowTitle.Length);
		if (length == 0)
		{
			return string.Empty;
		}
		return windowTitle.ToString();
	}

    private void OnExit(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        Application.Exit();
    }
}
