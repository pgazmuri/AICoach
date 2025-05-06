using System.Text.Json;
using System.Windows.Forms;

namespace AICoach.Services
{
    public class NotificationService
    {
        private readonly NotifyIcon _trayIcon;
        private string _activity = string.Empty;
        private string _suggestion = string.Empty;

        public NotificationService(NotifyIcon trayIcon)
        {
            _trayIcon = trayIcon;
            _trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;
        }

        private void TrayIcon_BalloonTipClicked(object? sender, EventArgs e)
        {
            this.ShowSuggestionDialog(_suggestion);
        }

        public void ShowAISuggestion(string response)
        {
            Logger.Instance.Log($"AI Suggestion: {response}");
            
            if (string.IsNullOrWhiteSpace(response) || response == "No Suggestion")
            {
                return;
            }

            try
            {
                // Try to parse as JSON
                if (TryParseJsonResponse(response, out string activity, out string suggestion))
                {
                    _activity = activity;
                    _suggestion = suggestion;

                    // Show balloon tip with activity
                    _trayIcon.BalloonTipTitle = "AI Coach";
                    _trayIcon.BalloonTipText = _activity;
                    _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                    _trayIcon.ShowBalloonTip(5000); // Show for 5 seconds
                }
                else
                {
                    // If not JSON or "No Suggestion", don't show anything
                    Logger.Instance.Log("Response was not valid JSON format or 'No Suggestion'");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log($"Error processing AI suggestion: {ex.Message}");
                ShowError($"Error processing suggestion: {ex.Message}");
            }
        }

        private bool TryParseJsonResponse(string response, out string activity, out string suggestion)
        {
            activity = string.Empty;
            suggestion = string.Empty;

            // Check for "No Suggestion" case
            if (response == "No Suggestion")
            {
                return false;
            }

            try
            {
                // Parse JSON
                using (JsonDocument document = JsonDocument.Parse(response))
                {
                    JsonElement root = document.RootElement;

                    // Extract required fields
                    if (root.TryGetProperty("activity", out JsonElement activityElement) &&
                        root.TryGetProperty("suggestion", out JsonElement suggestionElement))
                    {
                        activity = activityElement.GetString() ?? string.Empty;
                        suggestion = suggestionElement.GetString() ?? string.Empty;

                        // Verify we have both fields
                        if (!string.IsNullOrWhiteSpace(activity) && !string.IsNullOrWhiteSpace(suggestion))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Logger.Instance.Log($"JSON parsing error: {ex.Message}");
            }

            return false;
        }

        public void ShowSuggestionDialog(string suggestion)
        {
            MessageBox.Show(suggestion, "AI Coach Suggestion", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowError(string errorMessage)
        {
            _trayIcon.BalloonTipTitle = "AI Coach Error";
            _trayIcon.BalloonTipText = errorMessage;
            _trayIcon.BalloonTipIcon = ToolTipIcon.Error;
            _trayIcon.ShowBalloonTip(5000); // Show for 5 seconds
        }
    }
}