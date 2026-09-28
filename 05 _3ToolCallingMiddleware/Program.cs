using MaFUtilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Realtime;
using System.ClientModel;
using System.Text;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;


await CreatingTools.RunSample();



public static class CreatingTools
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


        PersonTools personTools = new PersonTools();
        //创建智能体，定义一个指令
        AIAgent agent = client.GetChatClient("gpt-4.1-mini").AsAIAgent(instructions: "想领导一样说话", tools: [
                AIFunctionFactory.Create(personTools.GetPersons,"get_persons","Get all the persons you know"),
                    AIFunctionFactory.Create(personTools.GetPerson,"get_person","Get a person by name"),
                    AIFunctionFactory.Create(ChangeConsoleColor,"change_console_color","Change the console color")
            ])
            .AsBuilder()
            .Use(Middleware)
            .Build();

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

    private static async ValueTask<object?> Middleware(AIAgent agent, FunctionInvocationContext context,Func<FunctionInvocationContext,CancellationToken,ValueTask<Object?>> next,CancellationToken cancellationToken)
    {
        StringBuilder toolDetails = new();
        toolDetails.Append($"(Args:{string.Join(",", context.Arguments.Select(x => $"[{x.Key}={x.Value}]"))})");
        Output.Yellow(toolDetails.ToString());
        return await next.Invoke(context, cancellationToken);
    }






    public record PersonInfo(string Name, string FavoriteColor);
    public class PersonTools
    {
        public PersonInfo[] GetPersons()
        {
            
            return GetData();
        }

        public PersonInfo? GetPerson(string name)
        {
            
            PersonInfo[] data = GetData();
            return data.FirstOrDefault(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        private static PersonInfo[] GetData()
        {
            return
            [
                    new PersonInfo("cc", "Red"),
                    new PersonInfo("Bob", "Blue"),
                    new PersonInfo("Charlie", "Green"),
                    new PersonInfo("cofer", "Yellow"),
            ];
        }


    }
    //改变颜色工具
    public static void ChangeConsoleColor(ConsoleColor color)
    {
        Output.Gray($"(ChangeConsoleColor was called with '{color}')");
        Console.ForegroundColor = color;
    }

}
