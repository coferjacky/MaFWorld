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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

await StructuredOutputInstructions.RunSample();
public static class StructuredOutputInstructions
{
    public static async Task RunSample()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        OpenAIClient client = new(
             new ApiKeyCredential(config["apiKey"]),
             new OpenAIClientOptions
             {
                 Endpoint = new Uri(config["endpoint"])
             });



        //创建智能体，定义一个指令
        AIAgent agent = client.GetChatClient("gpt-5-mini").AsAIAgent(instructions: "你很擅长从文本文件中提取数据，如人名，国籍，城市");
        string question = "中国水手陈海，来自福建泉州。他随远洋货轮穿梭于大洋之上，狂风卷起巨浪狠狠撞向船身。他稳稳扶着栏杆，仔细检查缆绳与设备。甲板满是咸腥海水，日复一日伴着轮机轰鸣值守。跑船远离故土，孤寂常伴左右，每当望见港口灯火，心中便念着泉州的家，静待归航。";

        Output.Title("Structured output call");
        AgentResponse<ExtractedDate> response = await agent.RunAsync<ExtractedDate>(question);

        ExtractedDate data = response.Result;

        Console.WriteLine($"- Name:{data.Name}");
        Console.WriteLine($"- Country:{data.Country}");
        Console.WriteLine($"- City:{data.City}");

        Console.WriteLine($"- Poem:{data.PoemAboutTheCountry}");
       


      

    }
    private class ExtractedDate
    {
        public required string Name { get; set; }
        [Description("说明该国家使用货币")]
        public required string Country { get; set; }
        [Description("说明该城市的总人口数量")]
        public required string City { get; set; }
        [Description("用日语写")]
        public required string PoemAboutTheCountry {  get; set; }
    }
}