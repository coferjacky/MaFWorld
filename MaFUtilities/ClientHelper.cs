
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using OpenAI;

namespace MaFUtilities;


/// <summary>
/// ClientHelper 就是个“造 OpenAI 客户端的工具”，三个方法分别连官方/Azure/中转站，加一个
/// showRawCall 开关能在请求发出时把原始 JSON 打印出来，方便你观察 SDK 到底发了什么。
/// </summary>
public static class ClientHelper
{
    //官方OpenAI 创建客户段
    public static OpenAIClient GetOpenAIClient(bool showRawCall, RawCallOptions? rawCall = null)
    {
        string apiKey = SecretsManager.GetOpenAICredentials();
        if (!showRawCall)
        {
            return new OpenAIClient(apiKey);
        }

        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Transport = new HttpClientPipelineTransport(new HttpClient(new CustomClientHttpHandler(rawCall ?? new RawCallOptions())))
        });
    }

    // 第三方 AI 中转站（OpenAI 兼容接口），端点和密钥来自 User Secrets 中的 OpenAiEndpoint / OpenAiApiKey
    public static OpenAIClient GetCustomOpenAIClient(bool showRawCall = false, RawCallOptions? rawCall = null)
    {
        (Uri endpoint, ApiKeyCredential apiKey) = SecretsManager.GetCustomOpenAICredentials();
        if (!showRawCall)
        {
            return new OpenAIClient(apiKey, new OpenAIClientOptions
            {
                Endpoint = endpoint
            });
        }

        return new OpenAIClient(apiKey, new OpenAIClientOptions
        {
            Endpoint = endpoint,
            //如果开启了 showRawCall，就把自定义的 CustomClientHttpHandler 放到 SDK 的 HttpClient 里，这样就能在请求发出时打印原始 JSON。
            Transport = new HttpClientPipelineTransport(new HttpClient(new CustomClientHttpHandler(rawCall ?? new RawCallOptions())))
        });
    }
    //按照官方文档，使用 Azure OpenAI 创建客户端
    public static OpenAIClient GetAzureOpenAIClient(bool showRawCall = false, RawCallOptions? rawCall = null)
    {
        (Uri endpoint, ApiKeyCredential apiKey) = SecretsManager.GetAzureOpenAICredentials(true);
        if (!showRawCall)
        {
            return new OpenAIClient(apiKey, new OpenAIClientOptions
            {
                Endpoint = endpoint
            });
        }

        return new OpenAIClient(apiKey, new OpenAIClientOptions
        {
            Endpoint = endpoint,
            Transport = new HttpClientPipelineTransport(new HttpClient(new CustomClientHttpHandler(rawCall ?? new RawCallOptions())))
        });
    }

    public class RawCallOptions
    {
        public bool ShowUrl { get; set; }           // 是否打印请求 URL
        public bool ShowRequest { get; set; } = true; // 是否打印请求体（默认开启）
        public bool ShowResponse { get; set; }        // 是否打印响应体
    }


   // 通过继承.NET 标准的 HttpClientHandler 并覆写 SendAsync 方法，实现对 HTTP 流量的拦截与输出：熟称 抓包器
    class CustomClientHttpHandler(RawCallOptions rawCallOptions) : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)  //CancellationToken cancellationToken 取消令牌
        {
            // 读取请求体,如果请求体是空，则返回null，如果不为null，则调用 ReadAsStringAsync 方法读取请求体的内容，并将其转换为字符串。这里使用了 null 条件运算符 ?. 来处理可能为 null 的情况。
            string requestString = await request.Content?.ReadAsStringAsync(cancellationToken)!;
            //如果开启了"显示 URL"，用绿色打印请求地址（含协议、域名、路径）。
            if (rawCallOptions.ShowUrl)
            {
                Utils.Green($"Raw Request ({request.RequestUri})");
            }
            //如果开启了"显示请求体"（默认开），用灰色打印请求正文（先经 MakePretty 美化），然后画一条分隔线。
            if (rawCallOptions.ShowRequest)
            {
                Utils.Gray(MakePretty(requestString));
                Utils.Separator();
            }
            //放行——调用父类方法真正把请求发给 AI 服务器，并等待响应。这是整个拦截器最关键的一行：只观察不干预。
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            //如果开启了"显示响应体"，读出响应正文并美化打印。这里不需要 ?.，因为响应一定有 body。
            if (rawCallOptions.ShowResponse)
            {
                string responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                Utils.Green("Raw Response");
                Utils.Gray(MakePretty(responseString));
                Utils.Separator();
            }

            return response;
        }
        //JSON 美化打印的小工具
        private string MakePretty(string input)
        {
            try
            { 
                JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(input);// 1. 解析 JSON
                return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true }); // 2. 带缩进重新输出
            }
            catch
            {
                return input;
            }
        }
    }

}