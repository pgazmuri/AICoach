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
        public async Task<string> GetAISuggestionFromHistoryAsync(IEnumerable<ScreenshotService.ScreenshotRecord> screenshotHistory, string prompt)
        {
            try
            {
                var screenshotCount = screenshotHistory.Count();
                Logger.Instance.Log($"Generating AI suggestion with {screenshotCount} screenshot(s)");
                
                // Build chat messages with all screenshots
                var messages = new List<ChatMessage>();
                
                // System message
                messages.Add(ChatMessage.CreateSystemMessage(
                    "You are an AI assistant analyzing a series of screenshots in chronological order to provide suggestions about how AI could be used to help perform the task shown in the screenshots. " +
                    "Use the chronological context to understand what the user is doing over time."));
                
                // Create user message with multiple screenshots
                var contentParts = new List<ChatMessageContentPart>();
                
                // Add the prompt with chronological context
                var promptBuilder = new StringBuilder(prompt);
                promptBuilder.AppendLine("\n\nThe following screenshots are in chronological order from oldest to newest:");
                
                int index = 1;
                foreach (var record in screenshotHistory)
                {
                    promptBuilder.AppendLine($"\nScreenshot {index}: Taken at {record.Timestamp}, Window Title: {record.WindowTitle}");
                    index++;
                }

                contentParts.Add(ChatMessageContentPart.CreateTextPart(promptBuilder.ToString()));
                
                // Add each screenshot as an image part
                foreach (var record in screenshotHistory)
                {
                    var imageBytes = new BinaryData(_screenshotService.ConvertScreenshotToBytes(record.Screenshot));
                    contentParts.Add(ChatMessageContentPart.CreateImagePart(imageBytes, "image/png"));
                }

                messages.Add(ChatMessage.CreateUserMessage(contentParts.ToArray()));

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