
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




            Output.Separator();
            Output.Title("Streaming Call");

            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("如何做牛肉汤?"))
            {
                Console.Write(update);
            }

            Output.Separator();

            Output.Title("Streaming Call (gathering all updates to a response at the end)");
            List<AgentResponseUpdate> updates = [];
            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("如何做牛肉汤?"))
            {
                updates.Add(update);
                Console.Write(update);
            }

            AgentResponse collectedResponse = updates.ToAgentResponse();
            //Use to the usage, and other return data...
            Console.WriteLine(collectedResponse.Usage!.OutputTokenCount);


        }
    }
}
