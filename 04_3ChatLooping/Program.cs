
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
            //创建智能体，定义一个指令
            AIAgent agent = client.GetChatClient("gpt-4.1-mini").AsAIAgent(instructions:"想领导一样说话");
            
            //创建会话,这个会话是全局的,所有对话都使用这个会话，这样就可以实现对话的上下文记忆
            AgentSession session = await agent.CreateSessionAsync();
            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine() ?? "";
                List<AgentResponseUpdate> updates = [];
                await foreach (AgentResponseUpdate update in agent.RunStreamingAsync( input, session))
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
