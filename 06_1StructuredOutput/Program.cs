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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;


await SturctureOutput.RunSample();
public static class SturctureOutput
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
        AIAgent agent = client.GetChatClient("gpt-4.1-mini").AsAIAgent(instructions: "你是一个电影专家");
          
        string question = "从IMDB网站列最高分的三部电影";

        Output.Title("Normal call");
        AgentResponse normalResponse = await agent.RunAsync(question);
        Output.Gray("normalResponse.Text=A variable foermat decided by th ai");
        Console.WriteLine(normalResponse);
        //你会发现 输出非常不稳定，你就难在如何去稳定输出，如何去稳定输出
        Output.Separator();


        //结构化输出
        Output.Title("Structured output call");
        AgentResponse<MovieResult> response = await agent.RunAsync<MovieResult>(question);

        Output.Gray("response.Result=.net object you can format as you see fit");
        MovieResult movieResult = response.Result;

        foreach(Movie movie in movieResult.Movies)
        {
            Console.WriteLine($"{movie.Title} - ({movie.YearOfRelease})- by -{movie.Director} -({movie.ImdbScore})");
        }


        Console.WriteLine(movieResult); 
    }

    private class MovieResult
    {
        public required List<Movie> Movies { get; set; }
    }


    private class Movie
    {

        public required string Title {  get; set; }

        public required string Director { get; set; }
        public required int YearOfRelease { get; set; }

        public required double ImdbScore { get; set; } 
    }


}

