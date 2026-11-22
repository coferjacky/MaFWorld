using A2A;
using OpenAI;
using Microsoft.Agents.AI;
using MaFUtilities;
using System.ClientModel;
using OpenAI.Chat;

Utils.Gray("Initializing");
Utils.Gray("- Waiting 1 sec for the server to be ready");
await Task.Delay(1000);
Secrets secrets = SecretsManager.GetSecrets();

Utils.Gray("- Connecting to Remote Agent");
//A2A卡片解析器
A2ACardResolver agentCardResolver = new A2ACardResolver(new Uri("http://localhost:5000/"));
AIAgent remoteAgent = await agentCardResolver.GetAIAgentAsync();

Utils.Separator();

Utils.Gray("Ready for questions");
(Uri endpoint, ApiKeyCredential apiKey) = SecretsManager.GetCustomOpenAICredentials();

OpenAIClient client = new(apiKey, new OpenAIClientOptions
{
    Endpoint = endpoint
});

ChatClientAgent agent = client
    .GetChatClient("gpt-5.4-mini")
    .AsAIAgent(
        name: "ClientAgent",
        instructions: "You specialize in handling queries for users and using your tools to provide answers.",
        tools: [remoteAgent.AsAIFunction()]);

AgentSession session = await agent.CreateSessionAsync();
while (true)
{
    Console.Write("> ");
    string message = Console.ReadLine() ?? string.Empty;
    if (message == string.Empty)
    {
        continue;
    }

    AgentResponse response = await agent.RunAsync(message, session);
    Console.WriteLine(response);
}