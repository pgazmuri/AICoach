# AICoach

AICoach is a productivity enhancement tool that observes your screen activity, analyzes it with AI, and provides contextual suggestions about how AI tools could help with your current tasks. It runs quietly in your system tray, taking periodic screenshots when you're active, and offers personalized AI assistance recommendations.

## Features

- **Automatic Activity Monitoring**: Takes periodic screenshots when you're active at your computer
- **Multi-Monitor Support**: Intelligently captures the screen where your cursor is located
- **AI-Powered Analysis**: Sends screenshots to OpenAI's vision models to understand what you're working on
- **Contextual Suggestions**: Offers relevant AI tool suggestions based on your current activities
- **Historical Context**: Analyzes sequences of screenshots to understand task progression over time
- **Minimal UI**: Runs in the system tray with minimal interaction required

## Requirements

- Windows operating system
- .NET 9.0 or later
- Azure OpenAI Service API key with GPT-4 Vision capabilities
- Internet connection for AI analysis

## Installation

1. Clone this repository:
   ```
   git clone https://github.com/yourusername/AICoach.git
   ```

2. Build the solution:
   ```
   cd AICoach
   dotnet build
   ```

3. Create an `appsettings.json` file based on the example:
   ```json
   {
     "OpenAI": {
       "ApiKey": "your-api-key",
       "Endpoint": "https://your-resource.openai.azure.com/",
       "DeploymentName": "your-gpt4-vision-deployment"
     }
   }
   ```

4. Run the application:
   ```
   dotnet run --project AICoach
   ```

## Usage

1. After starting AICoach, it will appear as an icon in your system tray
2. The app will automatically take screenshots every 60 seconds when you're active
3. Click the tray icon to access the menu:
   - **Analyze...** - Manually trigger an analysis
   - **Pause/Resume** - Toggle automatic screenshot capture
   - **Exit** - Close the application

4. When an AI suggestion is available, you'll receive a notification

## Configuration

You can customize the AI prompt by editing the `prompt.txt` file in the application directory. This file contains instructions for the AI on how to analyze your screenshots.

## Privacy & Security

- All screenshots are processed locally and only sent to the Azure OpenAI API
- No screenshots are permanently stored outside of your computer
- The application maintains a small rolling history of screenshots in memory
- No user data is collected or transmitted beyond what's needed for AI analysis

## How It Works

AICoach captures screenshots at regular intervals while you're active. It uses the Azure OpenAI API to analyze these screenshots and understand what you're doing. The AI model considers multiple screenshots to understand your workflow progression and suggests AI-powered tools that could help with your current tasks.

The app specifically focuses on detecting repetitive tasks, research patterns, content creation, and other activities where AI assistance could be beneficial.

## Building from Source

1. Ensure you have the .NET 9.0 SDK installed
2. Clone the repository
3. Run `dotnet build` to compile
4. Run `dotnet test` to execute the tests
5. Run `dotnet publish -c Release` to create a distributable package

## License

[MIT License](LICENSE)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.