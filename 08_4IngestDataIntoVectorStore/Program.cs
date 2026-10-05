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

await IngestDataIntoVectorStore.RunSample();
public static class IngestDataIntoVectorStore
{
    public static async Task RunSample()
    {
        //硬编码了一个包含 10 条问答（Q&A）的列表，模拟企业内部的员工手册或 FAQ（例如 Wi-Fi 密码、请假流程等）。
        List<KnowledgeBaseEntry> knowledgeBase =
        [
            new("What is the WI-FI Password at the Office?", "The Password is 'Guest42'"),
            new("Is Christmas Eve a full or half day off", "It is a full day off"),
            new("How do I register vacation?", "Go to the internal portal and under Vacation Registration (top right), enter your request. Your manager will be notified and will approve/reject the request"),
            new("What do I need to do if I'm sick?", "Inform you manager, and if you have any meetings remember to tell the affected colleagues/customers"),
            new("Where is the employee handbook?", "It is located [here](https://www.yourcompany.com/hr/handbook.pdf)"),
            new("Who is in charge of support?", "John Doe is in charge of support. His email is john@yourcompany.com"),
            new("I can't log in to my office account", "Take hold of Susan. She can reset your password"),
            new("When using the CRM System if get error 'index out of bounds'", "That is a known issue. Log out and back in to get it working again. The CRM team have been informed and status of ticket can be seen here: https://www.crm.com/tickets/12354"),
            new("What is the policy on buying books and online courses?", "Any training material under 20$ you can just buy.. anything higher need an approval from Richard"),
            new("Is there a bounty for find candidates for an open job position?", "Yes. 1000$ if we hire them... Have them send the application to jobs@yourcompany.com")
        ];

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

        #region Alternative Connectors

        //VectorStore vectorStoreFromAzureAiSearch = new AzureAISearchVectorStore(
        //    new SearchIndexClient(new Uri("azureAiSearchEndpoint"),
        //        new AzureKeyCredential("azureAiSearchKey")
        //    ));

        //VectorStore vectorStoreFromSqlServer2025 = new SqlServerVectorStore(
        //    "connectionString");

        //VectorStore vectorStoreFromCosmosDb = new CosmosNoSqlVectorStore(
        //    "connectionString",
        //    "databaseName",
        //    new CosmosClientOptions
        //    {
        //        UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default,
        //    });

        #endregion
        //获取名为 knowledge_base 的集合（类似于关系型数据库中的表） 此时压根就没有这个表。
        VectorStoreCollection<Guid, KnowledgeBaseVectorRecord> vectorStoreCollection = vectorStore.GetCollection<Guid, KnowledgeBaseVectorRecord>("knowledge_base");
        //框架会自动检查 SQLite 数据库中是否存在名为 knowledge_base 的表：
        //如果不存在，它会读取你传入的实体类 KnowledgeBaseVectorRecord 里的各种特性（比如[VectorStoreKey]、[VectorStoreData]），自动帮你把这张表建好。
        //如果存在，它就什么都不做，直接使用现有的表。
        await vectorStoreCollection.EnsureCollectionExistsAsync();

        Console.Write("Import Data? (Y/N): ");
        ConsoleKeyInfo key = Console.ReadKey();
        if (key.Key == ConsoleKey.Y)
        {
            //调用 EnsureCollectionDeletedAsync() 删除旧表，再调用 EnsureCollectionExistsAsync() 建新表
            await vectorStoreCollection.EnsureCollectionDeletedAsync();

            //Create anew
            await vectorStoreCollection.EnsureCollectionExistsAsync();
            Console.Clear();
            int counter = 0;
            foreach (KnowledgeBaseEntry entry in knowledgeBase)
            {
                counter++;
                Console.Write($"\rEmbedding Data: {counter}/{knowledgeBase.Count}");
                //UpsertAsync() 方法会先检查数据库中是否存在 Id 相同的记录，如果存在则更新，如果不存在则插入。同时调用 embeddingGenerator 将文本转换为向量（向模型自动请求了向量数组）。

                /**
                 * 当你执行 await UpsertAsync(...) 时，框架在底层的完整动作链是这样的：

                    1、准备拦截：框架发现你要往数据库里插数据了。

                    2、发现需求：它检查你传入的 KnowledgeBaseVectorRecord 对象，发现有一个标着 [VectorStoreVector] 的属性需要转成向量。

                    3、调用生成器（触发网络请求）：框架自动调用那个配置好的 embeddingGenerator，把你拼接好的文本（Q...A...）通过网络发送给 OpenAI 的 API。

                    4、异步等待：因为前面有 await 关键字，你的程序会在这里暂停等待，直到 OpenAI 处理完毕并把那 1536 个数字组成的数组通过网络传回你的电脑。

                    5、落盘保存：拿到数组后，框架再连同文本一起，执行真正的 SQLite 写入操作
                 
                 */






                await vectorStoreCollection.UpsertAsync(new KnowledgeBaseVectorRecord
                {
                    Id = Guid.NewGuid(),
                    Question = entry.Question,
                    Answer = entry.Answer,
                });
            }

            Console.WriteLine();
            Console.WriteLine("\rEmbedding complete...");
        }

        Console.WriteLine();

        Output.Title("Listing all data in the vector-store");
        //验证数据是否导入成功
        await foreach (KnowledgeBaseVectorRecord existingRecord in vectorStoreCollection.GetAsync(record => record.Id != Guid.Empty, int.MaxValue))
        {
            Console.WriteLine($"Q: {existingRecord.Question} - A: {existingRecord.Answer} - Vector: {existingRecord.Vector}");
        }
    }

    public record KnowledgeBaseEntry(string Question, string Answer);

    public class KnowledgeBaseVectorRecord
    {
        [VectorStoreKey]
        public required Guid Id { get; set; }

        [VectorStoreData]
        public required string Question { get; set; }

        [VectorStoreData]
        public required string Answer { get; set; }

        [VectorStoreVector(1536)]
        public string Vector => $"Q: {Question} - A: {Answer}";
    }
}