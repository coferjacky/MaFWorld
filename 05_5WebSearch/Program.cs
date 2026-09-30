using MaFUtilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Realtime;
using OpenAI.Responses;
using System.ClientModel;
using System.Text;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;


await WebSearch.Runsample();
public static class WebSearch
{
    public static async Task Runsample()
    {
        //建立连接
        IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        OpenAIClient client = new(
             new ApiKeyCredential(config["apiKey"]),

             new OpenAIClientOptions
             {
                 Endpoint = new Uri(config["endpoint"])
             });

#pragma warning disable OPENAI001
        //创建智能体，定义一个指令
        ChatClientAgent agent = client.GetResponsesClient()
#pragma warning restore OPENAI001
            .AsAIAgent(name: "main agent", instructions: "你是spacex的员工，随时跟踪每天的最新的SPACEX的新闻",
            model: "gpt-4.1-mini",
            tools:
            [
                AIFunctionFactory.Create(GetDateTimeUtc),
                AIFunctionFactory.Create(GetTimeZone),
                new HostedWebSearchTool()
            ]);

        //创建会话,这个会话是全局的,所有对话都使用这个会话，这样就可以实现对话的上下文记忆
        AgentSession session = await agent.CreateSessionAsync();
        Console.OutputEncoding = Encoding.UTF8;
        while (true)
        {
            Console.Write("> ");
            string input=Console.ReadLine()??"";
            AgentResponse respone = await agent.RunAsync(input, session);
            {
                Console.WriteLine(respone);
            }

            Output.Separator();
        }
    }
    public static DateTime GetDateTimeUtc() { 
        return DateTime.UtcNow;  
    }
    public static TimeZoneInfo GetTimeZone()
    {
        return TimeZoneInfo.Local;
    }

}