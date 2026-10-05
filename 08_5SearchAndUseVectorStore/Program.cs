using MaFUtilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Containers;
using OpenAI.Realtime;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using CommunityToolkit.VectorData.SqliteVec;


await SearchAndUseVectorStore.RunSample();

public static class SearchAndUseVectorStore
{
    public static async Task RunSample()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

        //初始化 OpenAIClient，并指定使用 text-embedding-3-small 模型。该模型的作用是将文本转换为高维向
        //量（可以理解为一长串浮点数，用来表示文本的语义特征）。

        OpenAIClient client = new(
             new ApiKeyCredential(config["apiKey"]),

             new OpenAIClientOptions
             {
                 Endpoint = new Uri(config["endpoint"])
             });


        //制作一个向量生成器
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client
            .GetEmbeddingClient("text-embedding-3-small")
            .AsIEmbeddingGenerator();

        //创建向量数据库
        string connectionString = $"Data Source={Path.GetTempPath()}\\af-course-vector-store.db";
        VectorStore vectorStore = new CommunityToolkit.VectorData.SqliteVec.SqliteVectorStore(connectionString, new SqliteVectorStoreOptions
        {
            //把 embeddingGenerator 传给配置选项，这样数据库在插入数据时，
            //可以自动调用大模型将文本转化为向量。
            EmbeddingGenerator = embeddingGenerator
        });

        //Get Vector Store Collection (so we can search against it)
        VectorStoreCollection<Guid, KnowledgeBaseVectorRecord> vectorStoreCollection = vectorStore.GetCollection<Guid, KnowledgeBaseVectorRecord>("knowledge_base");

        //创建一个智能体
        ChatClientAgent agent = client
            .GetChatClient("gpt-4.1-nano")
            .AsAIAgent(instructions: "你是公司内部知识库专家");
        //开启了一个持久的对话会话（Session），这样模型能够记住多轮对话的上下文。
        AgentSession session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("> ");
            string input = Console.ReadLine() ?? "";

            StringBuilder mostSimilarKnowledge = new StringBuilder();
            await foreach (VectorSearchResult<KnowledgeBaseVectorRecord> searchResult in vectorStoreCollection.SearchAsync(input, 3))
            {
                string searchResultAsQAndA = $"Q: {searchResult.Record.Question} - A: {searchResult.Record.Answer}";
                Output.Gray($"Search result [Score: {searchResult.Score}] {searchResultAsQAndA}");
                mostSimilarKnowledge.AppendLine(searchResultAsQAndA);
            }
            //程序并没有直接让 AI 回答用户的问题，而是把“刚刚检索到的相关知识”作为背景信息，
            //连同“用户的原始问题”一起发送给 AI。AI 收到后，会结合这些内部资料来准确回答用户
            //的提问，并输出到控制台。
            List<ChatMessage> messagesToSend =
            [
                new ChatMessage(ChatRole.User, "Here is the most relevant Knowledge base information: " + mostSimilarKnowledge),
                new ChatMessage(ChatRole.User, input)
            ];

            AgentResponse response = await agent.RunAsync(messagesToSend, session);
            {
                Output.Yellow("Final Answer after Search + LLM");
                Console.WriteLine(response);
            }

            Output.Separator();
        }
    }
}