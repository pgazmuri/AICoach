using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        public async Task<string> GetAISuggestionAsync(Bitmap screenshot, string prompt)
        {
            try
            {
                // Convert screenshot to bytes
                var imageBytes = new BinaryData(_screenshotService.ConvertScreenshotToBytes(screenshot));

                // Build chat messages
                var messages = new List<ChatMessage>
                {
                    ChatMessage.CreateSystemMessage(
                        "You are an AI assistant analyzing screenshots to provide suggestions about how AI could be used to help perform the task shown in the screenshot."),
                    ChatMessage.CreateUserMessage(new ChatMessageContentPart[] {
                        ChatMessageContentPart.CreateTextPart(prompt),
                        ChatMessageContentPart.CreateImagePart(imageBytes, "image/png")
                    })
                };

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