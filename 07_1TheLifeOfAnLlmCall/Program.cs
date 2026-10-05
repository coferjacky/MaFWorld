using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using MaFUtilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Containers;
using OpenAI.Realtime;
using OpenAI.Responses;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

//



await LifeOfAnLlmCall.RunSample();

public static class LifeOfAnLlmCall
{
    public static async Task RunSample()
    {
        //创建一个自定义的HttpHandler，用于输出请求和响应
        using CustomClientHttpHandler handler = new CustomClientHttpHandler();
        //创建一个HttpClient，并使用自定义的HttpHandler
        using HttpClient httpClient = new HttpClient(handler);

        IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        OpenAIClient client = new(
             new ApiKeyCredential(config["apiKey"]),
             new OpenAIClientOptions
             {
                 Endpoint = new Uri(config["endpoint"]),
                 //把httpClient放进SDK的传输管道，从此所有流量都先经过你的handler
                 Transport = new HttpClientPipelineTransport(httpClient)
             });


        ChatClientAgent agent = client
            .GetChatClient("gpt-4.1-mini")
            .AsAIAgent(
                tools: [AIFunctionFactory.Create(GetWeather)],
                instructions: "像海盗一样说话!"  //系统提示词
            );

        AgentResponse<WeatherResponse> response = await agent.RunAsync<WeatherResponse>("巴黎的天气怎么样?");
        WeatherResponse result = response.Result;
    }

    class WeatherResponse
    {
        public required string City { get; set; }
        public required string Condition { get; set; }
        public required int DegreesFahrenheit { get; set; }
        public required int DegreesCelsius { get; set; }
    }

    public static string GetWeather(string city)
    {
        return "It is Sunny and 19 degrees today";
    }

    //窃听器本体
    class CustomClientHttpHandler : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //发送前：读出请求体，打印 URL 和美化后的 JSON。
            string requestString = await request.Content?.ReadAsStringAsync(cancellationToken)!;
            Output.Green($"Raw Request ({request.RequestUri})");
            Output.Gray(MakePretty(requestString));
            Output.Separator();

            //放行：调用 base.SendAsync 真正发出请求。注意必须 await 等响应回来，不能截胡。
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            //收到后：打印响应体，把 response 原样返回——SDK 完全不知道自己被监听了，功能不受任何影响。
            string responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            Output.Green("Raw Response");
            Output.Gray(MakePretty(responseString));
            Output.Separator();
            return response;
        }
        //把单行压缩的 JSON 解析后带缩进重新输出，方便人眼阅读。解析失败（如 SSE 流式数据）就直接返回原文
        private string MakePretty(string input)
        {
            try
            {
                JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(input);
                return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception e)
            {
                return input;
            }
        }
    }
}