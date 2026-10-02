using MaFUtilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Containers;
using OpenAI.Realtime;
using OpenAI.Responses;
using System.ClientModel;
using System.Runtime.InteropServices;
using System.Text;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;




await CodeInterpreter.RunSample();

#pragma warning disable OPENAI001
/// <summary>
/// 
/// 通过提示词来实现代码解释器（绘制图标）
/// </summary>

public static class CodeInterpreter
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


        //创建智能体，定义一个指令
        ChatClientAgent agent = client.GetResponsesClient()

            .AsAIAgent(name: "main agent", instructions: "你可以通过'code_interperter'工具来绘制图表",
            model: "gpt-5-nano",
            tools:
            [
               new HostedCodeInterpreterTool()
            ]);

        //创建会话,这个会话是全局的,所有对话都使用这个会话，这样就可以实现对话的上下文记忆
        AgentSession session = await agent.CreateSessionAsync();

        Console.OutputEncoding= Encoding.UTF8;
        while (true)
        {
            Console.Write("> ");
            string input =Console.ReadLine()??"";
            AgentResponse respone = await agent.RunAsync(input, session);
            {
                Console.WriteLine(respone);
                foreach (ChatMessage message in respone.Messages)
                {
                    foreach(AIContent content in message.Contents)
                    {
                        foreach(AIAnnotation annotation in content.Annotations ?? [])
                        {
                            if(annotation.RawRepresentation is ContainerFileCitationMessageAnnotation containerFileCitation)
                            {
                                //原始客户端拿到容器客户端
                                ContainerClient containerClient = client.GetContainerClient();

                                ClientResult<BinaryData>   fileContent = await containerClient.DownloadContainerFileAsync(containerFileCitation.ContainerId, containerFileCitation.FileId);
                               
                               
                                string path = Path.Combine(Path.GetTempPath(), containerFileCitation.Filename);
                                await File.WriteAllBytesAsync(path, fileContent.Value.ToArray());
                                //完成后，通过C#来运行以下部分
                                await Task.Factory.StartNew(() => 
                                {

                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = path,
                                        UseShellExecute = true
                                    });


                                });                         
                            }
                        }
                    }
                }

            }
            
        }

        Output.Separator();

    }
}
#pragma warning restore OPENAI001