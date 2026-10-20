using System.Text.Json;
using MaFUtilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

/// <summary>
//使用基于 Microsoft.Extensions.AI 的代理模型（Agent）来实现带持久化记忆的多轮对话。其核心机制是将对话的会话状态（Session）序列化，
//并通过自定义的 Provider 将聊天记录（Chat History）持久化到本地文件系统中，从而实现跨应用生命周期的上下文恢复。
/// </summary>

//Console.Clear();
Secrets secrets=SecretsManager.GetSecrets();    
// 获取OpenAI客户端
OpenAIClient client = ClientHelper.GetCustomOpenAIClient(); 
// 获取能保存记忆能力的ChatClientAgent
ChatClientAgent agent=client.GetChatClient("gpt-4.1-nano").AsAIAgent(new ChatClientAgentOptions
{
    //放一个自定义的消息存储器
    ChatHistoryProvider = new MyMessageStore()

});
// 创建会话
AgentSession session = await agent.CreateSessionAsync();
// 运行会话
AgentResponse response = await agent.RunAsync("谁是Obama", session);
Console.WriteLine(response);
// 序列化会话（把当前的对话状态（比如聊到了哪里、对话的唯一 ID 是什么）打包成 JSON 格式，并打印出这个对话的 ID。这就像存了个档。）
JsonElement sessionElement = await agent.SerializeSessionAsync(session);
Console.WriteLine(sessionElement.GetProperty("stateBag").GetProperty("SessionId").GetString());

//转换成纯字符串
string toStoreForTheUser = JsonSerializer.Serialize(sessionElement);

Utils.Separator();

//Some time passes....  模拟过了一段是时间
Utils.Green("Some time passes, and we restore the session...");

//字符串-》Json对象
JsonElement restoredSessionElement = JsonSerializer.Deserialize<JsonElement>(toStoreForTheUser);
//JSON对象-》会话对象
AgentSession restoredThread = await agent.DeserializeSessionAsync(restoredSessionElement);

AgentResponse someTimeLaterResponse = await agent.RunAsync("How Tall is he?", restoredThread);
Console.WriteLine(someTimeLaterResponse);

//每次 RunAsync 发请求前调用它,规定了记忆到底存放在哪，怎么读，怎么写
public class MyMessageStore : ChatHistoryProvider
{
    public string? SessionId { get; set; }

    public string SessionPath => Path.Combine(Path.GetTempPath(), $"{SessionId}.json");

    private readonly List<ChatMessage> _messages = [];
    // 从文件中读取消息历史记录。每次你对 AI 说完话，系统在把你的话传给大模型之前，都会自动先调用这个方法来加载旧记忆。
    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (context.Session!.StateBag.TryGetValue("SessionId", out string? sessionId))
        {
            SessionId = sessionId!;
        }
        else
        {
            SessionId = Guid.NewGuid().ToString();
            context.Session.StateBag.SetValue("SessionId", SessionId);
        }
        if (!File.Exists(SessionPath))
        {
            return [];
        }

        string json = await File.ReadAllTextAsync(SessionPath, cancellationToken);
        return JsonSerializer.Deserialize<List<ChatMessage>>(json)!;
    }
    //AI回答完毕后执行
    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        // Add both request and response messages to the store
        // Optionally messages produced by the AIContextProvider can also be persisted (not shown).
        _messages.AddRange(context.RequestMessages.Concat(context.ResponseMessages ?? []));

        await File.WriteAllTextAsync(SessionPath, JsonSerializer.Serialize(_messages), cancellationToken);
    }
}