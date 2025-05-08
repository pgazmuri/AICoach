using System.Text.Json;
using System.Windows.Forms;

namespace AICoach.Services
{
    public class NotificationService
    {
        private readonly NotifyIcon _trayIcon;
        private string _activity = string.Empty;
        private string _suggestion = string.Empty;
        private string _prompt = string.Empty;

        public NotificationService(NotifyIcon trayIcon)
        {
            _trayIcon = trayIcon;
            _trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;
        }

        private void TrayIcon_BalloonTipClicked(object? sender, EventArgs e)
        {
            ShowSuggestionDialog(_suggestion, _prompt);
        }

        public string ShowAISuggestion(string response)
        {
            Logger.Instance.Log($"AI Suggestion: {response}");
            
            if (string.IsNullOrWhiteSpace(response) || response == "No Suggestion")
            {
                return "";
            }

            try
            {
                // Try to parse as JSON
                if (TryParseJsonResponse(response, out string activity, out string suggestion, out string prompt))
                {
                    _activity = activity;
                    _suggestion = suggestion;
                    _prompt = prompt;

                    // Show balloon tip with activity
                    _trayIcon.BalloonTipTitle = "AI Coach";
                    _trayIcon.BalloonTipText = _activity;
                    _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                    _trayIcon.ShowBalloonTip(5000); // Show for 5 seconds

					return suggestion; // Return the suggestion for further processing if needed
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
			
			return ""; // Return empty string if no valid suggestion was found
        }

        private bool TryParseJsonResponse(string response, out string activity, out string suggestion, out string prompt)
        {
            prompt = string.Empty;
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
                        root.TryGetProperty("suggestion", out JsonElement suggestionElement) &&
                        root.TryGetProperty("prompt", out JsonElement promptElement))
                    {
                        activity = activityElement.GetString() ?? string.Empty;
                        suggestion = suggestionElement.GetString() ?? string.Empty;
                        prompt = promptElement.GetString() ?? string.Empty;

                        // Verify we have both fields, we may not have a prompt
                        // but we should have an activity and suggestion
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

        public void ShowSuggestionDialog(string suggestion, string prompt)
        {
            SuggestionPanel suggestionPanel = new SuggestionPanel(suggestion, prompt);
            suggestionPanel.ShowDialog();
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