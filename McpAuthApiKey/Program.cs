using OpenAI;
using MaFUtilities;
using System.ClientModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

OpenAIClient client = MaFUtilities.ClientHelper.GetCustomOpenAIClient();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(client);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app=builder.Build();

const string expectedApiKey = "MySecretKey";
app.MapMcp("/mcp").AddEndpointFilter(async (context, next) => {

    if (!string.Equals(context.HttpContext.Request.Headers["x-api-key"], expectedApiKey, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }
    return await next(context);

});

app.UseHttpsRedirection();

app.Run();  