using System.Text.Json;
using MaFUtilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using OpenTelemetry;          //Sdk.CreateTracerProviderBuilder() 所在的命名空间
using OpenTelemetry.Trace;    //AddConsoleExporter() 扩展方法所在的命名空间
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;


//Setup Telemetry
string sourceName = "AiSource";
TracerProviderBuilder tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter();  //把追踪数据打印到控制台，本地调试够用了

//如果需要把数据送到 Jaeger / Grafana / Zipkin 等任意 OTLP 后端，取消下面注释
//（需额外引用 NuGet 包：OpenTelemetry.Exporter.OpenTelemetryProtocol）
// .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));

using TracerProvider tracerProvider = tracerProviderBuilder.Build();

//第三方 AI 服务（OpenAI 兼容接口），端点与密钥来自 User Secrets 的 OpenAiEndpoint / OpenAiApiKey
OpenAIClient client = ClientHelper.GetCustomOpenAIClient();

AIAgent agent = client
    .GetChatClient("gpt-4.1")
    .AsAIAgent(
        name: "MyObservedAgent",
        instructions: "You are a Friendly AI Bot, answering questions")
    .AsBuilder()
    .UseOpenTelemetry(sourceName, options =>
    {
        options.EnableSensitiveData = true; //If the actual messages should be logged or not
    })
    .Build();

AgentSession session = await agent.CreateSessionAsync();

AgentResponse response1 = await agent.RunAsync("Hello, My name is Rasmus", session);
Utils.Red(response1.Text);
Utils.Separator();

AgentResponse response2 = await agent.RunAsync("What is the capital of France?", session);
Utils.Red(response2.Text);
Utils.Separator();

AgentResponse response3 = await agent.RunAsync("What was my name?", session);
Utils.Red(response3.Text);
Utils.Separator();