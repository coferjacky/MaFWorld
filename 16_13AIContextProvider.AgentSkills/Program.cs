using AgentSkills;
using OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using MaFUtilities;

#pragma warning disable MAAI001
Utils.Init("AI Context Provider (AgentSkills)");
OpenAIClient client = MaFUtilities.ClientHelper.GetCustomOpenAIClient();

string skillPath = "TestData\\AgentSkills";

AIAgent agent = client.GetChatClient("gpt-5.4-mini").AsAIAgent(new ChatClientAgentOptions
{
    AIContextProviders = [new AgentSkillsProvider(skillPath)],
    ChatOptions = new ChatOptions
    {
        Tools = [AIFunctionFactory.Create(PythonRunner.RunPhytonScript, name: "execute_python")]
    }
}).AsBuilder().Use(Utils.ToolCallingMiddleware).Build();


await Utils.RunChatLoopWithSession(agent);