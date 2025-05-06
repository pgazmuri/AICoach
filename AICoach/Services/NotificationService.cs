using System.Windows.Forms;

namespace AICoach.Services
{
    public class NotificationService
    {
        private readonly NotifyIcon _trayIcon;
		private string _suggestion = string.Empty;

        public NotificationService(NotifyIcon trayIcon)
        {
            _trayIcon = trayIcon;
			_trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;
        }

		private void TrayIcon_BalloonTipClicked(object? sender, EventArgs e){
			this.ShowSuggestionDialog(_suggestion);
		}

        public void ShowAISuggestion(string suggestion)
        {
			_suggestion = suggestion ?? "No Suggestion";
            Logger.Instance.Log($"AI Suggestion: {suggestion}");
            
            if (suggestion == "No Suggestion")
            {
                return;
            }

            _trayIcon.BalloonTipTitle = "AI Coach";
            _trayIcon.BalloonTipText = _suggestion;
            _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            _trayIcon.BalloonTipClicked -= (s, e) => { }; // Unsubscribe previous event handlers to avoid duplicates
            _trayIcon.ShowBalloonTip(5000); // Show for 5 seconds
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