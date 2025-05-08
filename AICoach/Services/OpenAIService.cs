using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace AICoach.Services
{
    public class OpenAIService
    {
        private readonly ChatClient _chatClient;
        private readonly ScreenshotService _screenshotService;

        public OpenAIService(ConfigurationService configService, ScreenshotService screenshotService)
        {
            _screenshotService = screenshotService;
            
            var openAiApiKey = configService.GetOpenAIApiKey();
            var endpoint = new Uri(configService.GetOpenAIEndpoint());
            var deploymentName = configService.GetOpenAIDeploymentName();
            
            var credential = new AzureKeyCredential(openAiApiKey);
            _chatClient = new AzureOpenAIClient(endpoint, credential).GetChatClient(deploymentName);
        }

        // Maintain backward compatibility with single screenshot
        public async Task<string> GetAISuggestionAsync(Bitmap screenshot, string prompt)
        {
            // Create a temporary record for this screenshot
            var windowTitle = _screenshotService.GetActiveWindowTitle();
            var record = new ScreenshotService.ScreenshotRecord(screenshot, windowTitle);
            return await GetAISuggestionFromHistoryAsync(new[] { record }, prompt);
        }

        // New method using screenshot history
        public async Task<string> GetAISuggestionFromHistoryAsync(IEnumerable<ScreenshotService.ScreenshotRecord> screenshotHistory, string prompt, List<string> _previousSuggestions = null)
        {
            try
            {
                var screenshotRecords = screenshotHistory.ToList();
                var screenshotCount = screenshotRecords.Count;
                Logger.Instance.Log($"Generating AI suggestion with {screenshotCount} screenshot(s)");
                
                // Build chat messages with screenshots as separate messages
                var messages = new List<ChatMessage>();
                
                // System message
                messages.Add(ChatMessage.CreateSystemMessage(
                    "You are an AI assistant analyzing a series of screenshots in chronological order to provide suggestions about how AI could be used to help perform the task shown in the screenshots. " +
                    "Use the chronological context to understand what the user is doing over time."));
                
                // Add the initial prompt with explanation
                messages.Add(ChatMessage.CreateUserMessage(
                    $"{prompt}\n\nI'll now share {screenshotCount} screenshots in chronological order from oldest to newest. " +
                    "Each screenshot represents what I was working on at different points in time."));
                
                // Add each screenshot as a separate message with metadata
                for (int i = 0; i < screenshotRecords.Count; i++)
                {
                    var record = screenshotRecords[i];
                    var isLastScreenshot = (i == screenshotRecords.Count - 1);
                    
					var previousSuggestions = _previousSuggestions != null && _previousSuggestions.Count > 0 ? string.Join(", ", _previousSuggestions) : "(No previous suggestions)";

                    // Create message text based on position
                    string messageText;
                    if (isLastScreenshot)
                    {
                        messageText = $"This is the most recent screenshot, taken at {record.Timestamp}, Window: \"{record.WindowTitle}\". " +
                                      "Only suggest AI-powered features that directly relate to what I'm doing in this final screenshot. " +
                                      "If a suggestion isn't clearly relevant to what's shown here, don't include it." +
									  $"If the suggestion is similar to a previous one, don't repeat it. Previous suggestions are: {previousSuggestions}. Do not give similar suggestions to those." +
									  "If you have no suggestions, say 'No Suggestion'.";
					}
					else if (i == 0)
					{
						messageText = $"This is the first screenshot, taken at {record.Timestamp}, Window: \"{record.WindowTitle}\". " +
									  "Please analyze the screenshots in chronological order and suggest AI-powered features that could help with the tasks shown.";
                    }
                    else
                    {
                        messageText = $"Screenshot {i+1} of {screenshotCount}, taken at {record.Timestamp}, Window: \"{record.WindowTitle}\"";
                    }

					//log message text
					Logger.Instance.Log($"Message text: {messageText}");
                    
                    // Create content parts with text and image
                    var contentParts = new List<ChatMessageContentPart>
                    {
                        ChatMessageContentPart.CreateTextPart(messageText),
                        ChatMessageContentPart.CreateImagePart(
                            new BinaryData(_screenshotService.ConvertScreenshotToBytes(record.Screenshot)), 
                            "image/png")
                    };
                    
                    messages.Add(ChatMessage.CreateUserMessage(contentParts.ToArray()));
                }

                // Send to OpenAI via the SDK
                var response = await _chatClient.CompleteChatAsync(messages);
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
    }
}