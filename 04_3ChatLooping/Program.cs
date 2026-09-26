
using MaFUtilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace _04_3ChatLooping
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //建立连接
            IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
            OpenAIClient client = new(
                 new ApiKeyCredential(config["apiKey"]),

                 new OpenAIClientOptions
                 {
                     Endpoint = new Uri(config["endpoint"])
                 });
            //创建智能体
            AIAgent agent = client.GetChatClient("gpt-4.1-mini").AsAIAgent();
            AgentSession session = await agent.CreateSessionAsync();
            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine() ?? "";
                List<AgentResponseUpdate> updates = [];
                await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(input, session))
                {
                    updates.Add(update);
                    Console.Write(update);
                }
                AgentResponse response = updates.ToAgentResponse();
                if (response.Usage != null)
                {
                    Console.WriteLine();
                    Output.Gray($"Tokens - In: {response.Usage.InputTokenCount} - Out: {response.Usage.OutputTokenCount}");
                }

                InMemoryChatHistoryProvider? historyProvider = agent.GetService<InMemoryChatHistoryProvider>();
                IList<ChatMessage> messagesForSession = historyProvider?.GetMessages(session) ?? [];

                Output.Separator();

            }
        }
    }
}
