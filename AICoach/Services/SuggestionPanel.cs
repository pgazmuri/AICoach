using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;

namespace AICoach.Services
{
    public class SuggestionPanel : Form
    {
        private string _prompt;
        private TextBox _promptTextBox;

        public SuggestionPanel(string suggestion, string prompt)
        {
            _prompt = prompt;
            
            this.Text = "AI Coach Suggestion";
            this.Size = new Size(450, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Information;

            // Create the layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 3,
                ColumnCount = 1
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F)); // Suggestion section (fixed height)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Prompt section (fills remaining space)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Button section (fixed height)

            // Suggestion label (top)
            Label suggestionLabel = new Label
            {
                Text = suggestion,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                AutoSize = false,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5),
                AutoEllipsis = true
            };
            Panel suggestionPanel = new Panel { Dock = DockStyle.Fill };
            suggestionPanel.Controls.Add(suggestionLabel);
            mainLayout.Controls.Add(suggestionPanel, 0, 0);

            // Prompt section (middle)
            Panel promptPanel = new Panel { Dock = DockStyle.Fill };
            
            Label promptLabel = new Label
            {
                Text = "Prompt for Copilot:",
                AutoSize = true,
                Location = new Point(0, 0),
                Font = new Font("Segoe UI", 9)
            };
            promptPanel.Controls.Add(promptLabel);

            _promptTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = false,
                ScrollBars = ScrollBars.Vertical,
                Text = prompt,
                Dock = DockStyle.None,
                Location = new Point(0, promptLabel.Bottom + 2),
                Width = promptPanel.ClientSize.Width,
                Height = promptPanel.ClientSize.Height - promptLabel.Height - 4,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            promptPanel.Controls.Add(_promptTextBox);

            mainLayout.Controls.Add(promptPanel, 0, 1);

            // Button panel (bottom)
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 3, 0, 0)
            };

            Button closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.Cancel,
                Size = new Size(80, 30),
            };
            
            Button copyButton = new Button
            {
                Text = "Copy Prompt",
                Size = new Size(100, 30),
                Margin = new Padding(0, 0, 10, 0)
            };
            copyButton.Click += (s, e) => CopyPromptToClipboard();

            buttonPanel.Controls.Add(closeButton);
            buttonPanel.Controls.Add(copyButton);
            
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            this.Controls.Add(mainLayout);
            this.CancelButton = closeButton;
            
            // Handle resize events
            this.Resize += SuggestionPanel_Resize;
            promptPanel.Resize += (s, e) => ResizePromptTextBox(promptPanel);
            this.Load += (s, e) => ResizePromptTextBox(promptPanel);
        }

        private void SuggestionPanel_Resize(object? sender, EventArgs e)
        {
            // Invalidate layout to ensure proper sizing
            this.PerformLayout();
        }

        private void ResizePromptTextBox(Panel parent)
        {
            if (_promptTextBox != null && parent != null)
            {
                Label? promptLabel = parent.Controls.OfType<Label>().FirstOrDefault();
                if (promptLabel != null)
                {
                    _promptTextBox.Width = parent.ClientSize.Width;
                    _promptTextBox.Height = parent.ClientSize.Height - promptLabel.Height - 4;
                }
            }
        }
        
        private void CopyPromptToClipboard()
        {
            try
            {
                // Get the current text from the textbox instead of the initial prompt
                string textToCopy = _promptTextBox.Text;
                if (!string.IsNullOrEmpty(textToCopy))
                {
                    Clipboard.SetText(textToCopy);
                    MessageBox.Show("Prompt copied to clipboard.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}