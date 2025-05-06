using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AICoach.Services
{
    public class ConfigurationService
    {
        private readonly Dictionary<string, Dictionary<string, string>> _config;

        public ConfigurationService()
        {
            try
            {
                _config = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
                    File.ReadAllText("appsettings.json")) 
                    ?? throw new InvalidOperationException("Failed to deserialize configuration.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration: {ex.Message}", ex);
            }
        }

        public string GetOpenAIApiKey()
        {
            return _config?["AzureOpenAI"]?["ApiKey"] 
                ?? throw new InvalidOperationException("OpenAI API key is missing.");
        }

        public string GetOpenAIEndpoint()
        {
            return _config?["AzureOpenAI"]?["Endpoint"] 
                ?? throw new InvalidOperationException("OpenAI Endpoint is missing.");
        }

        public string GetOpenAIDeploymentName()
        {
            return _config?["AzureOpenAI"]?["DeploymentName"] 
                ?? "gpt-4.1-mini"; // Default value as fallback
        }
    }
}