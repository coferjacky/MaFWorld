namespace Shared;

public record Secrets(
    string OpenAiApiKey,
    string AzureOpenAiEndpoint,
    string AzureOpenAiKey,
    string MicrosoftFoundryEndpoint,
    string AzureAiFoundryAgentId,
    string BingApiKey,
    string GitHubPatToken,
    string HuggingFaceApiKey,
    string TypeSafeApiKey,
    string OpenRouterApiKey,
    string ApplicationInsightsConnectionString,
    string GoogleGeminiApiKey,
    string XAiGrokApiKey,
    string TrelloApiKey,
    string TrelloToken,
    string AnthropicApiKey,
    string MistralApiKey,
    string AmazonBedrockApiKey,
    string OpenWeatherApiKey,
    string Mem0ApiKey);
