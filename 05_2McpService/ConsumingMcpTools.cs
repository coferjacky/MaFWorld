using MaFUtilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using ModelContextProtocol.Client;
using System.Net;


namespace _05_2McpService
{
    public static class ConsumingMcpTools
    {
        public static async Task RunSample()
        {
            //建立连接
            IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
            OpenAIClient client = new(
                 new ApiKeyCredential(config["apiKey"]),
                 new OpenAIClientOptions
                 {
                     Endpoint = new Uri(config["endpoint"])
                 });
            //调用MCP服务
            await using McpClient mcpClient =await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions { 
                Endpoint=new Uri("https://learn.microsoft.com/api/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
            IList<McpClientTool> mcpTools=await mcpClient.ListToolsAsync();



            //创建智能体，定义一个指令
            AIAgent agent = client.GetChatClient("gpt-4.1-mini").AsAIAgent(
                instructions: "你是微软agent framework C#专家" +
                                "(用工具找到你的知识)" +
                                "假设使用了带API密钥的OpenAi"

                ,tools: mcpTools.Cast<AITool>().ToList()
                );
            //创建会话,这个会话是全局的,所有对话都使用这个会话，这样就可以实现对话的上下文记忆
            AgentSession session = await agent.CreateSessionAsync();
            Console.OutputEncoding = Encoding.UTF8;
            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine() ?? "";
                AgentResponse respone = await agent.RunAsync(input, session);
                {
                    Console.WriteLine(respone);
                }
                Output.Separator();
            }
        }
    }
}
