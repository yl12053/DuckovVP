using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DuckovVP;

public class HttpDumpHandler : DelegatingHandler
{
    public HttpDumpHandler(HttpMessageHandler innerHandler = null)
        : base(innerHandler ?? new HttpClientHandler())
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // 1. Dump Request
        string requestDump = await DumpRequestAsync(request);
        Debug.Log("========== [HTTP REQUEST] ==========");
        Debug.Log(requestDump);
        Debug.Log("====================================\n");

        // 2. 发送请求
        var response = await base.SendAsync(request, cancellationToken);

        // 3. Dump Response
        string responseDump = await DumpResponseAsync(response);
        Debug.Log("========== [HTTP RESPONSE] ==========");
        Debug.Log(responseDump);
        Debug.Log("=====================================\n");

        return response;
    }

    private async Task<string> DumpRequestAsync(HttpRequestMessage request)
    {
        var sb = new StringBuilder();

        // Request Line
        sb.AppendLine($"{request.Method} {request.RequestUri.PathAndQuery} HTTP/{request.Version}");
        sb.AppendLine($"Host: {request.RequestUri.Authority}");

        // Headers
        foreach (var header in request.Headers)
        {
            sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            sb.AppendLine(); // 请求头和 Body 之间的空行

            // 读取 Body (注意：缓冲读取，防止数据流损坏)
            await request.Content.LoadIntoBufferAsync();
            string body = await request.Content.ReadAsStringAsync();
            sb.AppendLine(body);
        }

        return sb.ToString();
    }

    private async Task<string> DumpResponseAsync(HttpResponseMessage response)
    {
        var sb = new StringBuilder();

        // Status Line
        sb.AppendLine($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");

        // Headers
        foreach (var header in response.Headers)
        {
            sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        if (response.Content != null)
        {
            foreach (var header in response.Content.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            sb.AppendLine(); // 响应头和 Body 之间的空行

            // 读取 Body
            await response.Content.LoadIntoBufferAsync();
            string body = await response.Content.ReadAsStringAsync();
            sb.AppendLine(body);
        }

        return sb.ToString();
    }
}