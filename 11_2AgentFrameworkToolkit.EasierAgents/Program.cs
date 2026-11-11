using _11_2AgentFrameworkToolkit.EasierAgents;
using MaFUtilities;

Console.Clear();

Utils.Green("Before");
await Before.RunAsync();

Utils.Separator();

Utils.Green("After");
await After.RunAsync();