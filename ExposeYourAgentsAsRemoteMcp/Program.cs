using System.ClientModel;
using OpenAI;
using MaFUtilities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;



OpenAIClient client = MaFUtilities.ClientHelper.GetCustomOpenAIClient();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(client);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

WebApplication app = builder.Build();

app.MapMcp("/mcp");

app.UseHttpsRedirection();

app.Run();