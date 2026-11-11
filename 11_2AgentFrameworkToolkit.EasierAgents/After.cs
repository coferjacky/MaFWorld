
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MaFUtilities;
using MaFUtilities.Extensions;

namespace _11_2AgentFrameworkToolkit.EasierAgents;

public class After
{
    public static async Task RunAsync()
    {
        //Weather-Task: With GPT-5-Mini in low Reasoning, Call Tool with middleware and return as structured Output

       // Secrets secrets = SecretsManager.GetSecrets();

        var credentials = SecretsManager.GetCustomOpenAICredentials();

        OpenAIAgentFactory agentFactory = new(new OpenAIConnection
        {
            Endpoint = credentials.endpoint.ToString(),
            ApiKey = SecretsManager.GetOpenAICredentials()
        });

        OpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions
        {
            Model = OpenAIChatModels.Gpt5Mini,
            ReasoningEffort = OpenAIReasoningEffort.Low,
            Tools = [AIFunctionFactory.Create(WeatherTool.GetWeather)],
            RawToolCallDetails = details => { Utils.Gray(details.ToString()); }
        });

        AgentResponse<WeatherReport> response = await agent.RunAsync<WeatherReport>("What is the Weather like in Paris");
        WeatherReport weatherReport = response.Result;
        Console.WriteLine("City: " + weatherReport.City);
        Console.WriteLine("Condition: " + weatherReport.Condition);
        Console.WriteLine("Degrees: " + weatherReport.Degrees);
        Console.WriteLine("Fahrenheit: " + weatherReport.Fahrenheit);
        response.Usage.OutputAsInformation();
    }
}