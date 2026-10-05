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

await SearchAsTool.RunSample();


//使用工具的好处是，如果你问一些和知识库没有关系的数据

public static class SearchAsTool
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

        SearchTool searchTool = new SearchTool(vectorStoreCollection);
        //创建一个智能体
        ChatClientAgent agent = client
            .GetChatClient("gpt-4.1-mini")
            .AsAIAgent(instructions: "你是公司内部知识库专家(使用'search_knowledge'工具)", tools: [AIFunctionFactory.Create(searchTool.Search,"search_knowledge")]);

        AgentSession session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("> ");
            string input = Console.ReadLine() ?? "";           

         
            AgentResponse response = await agent.RunAsync(input, session);
            {
               
                Console.WriteLine(response);
            }

            Output.Separator();
        }
    }

    private class SearchTool(VectorStoreCollection<Guid,KnowledgeBaseVectorRecord> vectorStoreCollection)
    {
        public async Task<string> Search(string input)
        {
            StringBuilder mostSimilarKnowledge = new StringBuilder();
            await foreach (VectorSearchResult<KnowledgeBaseVectorRecord> searchResult in vectorStoreCollection.SearchAsync(input, 3))
            {
                string searchResultAsQAndA = $"Q: {searchResult.Record.Question} - A: {searchResult.Record.Answer}";
                Output.Gray($"Search result [Score: {searchResult.Score}] {searchResultAsQAndA}");
                mostSimilarKnowledge.AppendLine(searchResultAsQAndA);
            }
            Console.WriteLine();
            return mostSimilarKnowledge.ToString();
        }
    }


}