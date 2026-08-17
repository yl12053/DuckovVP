using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DuckovVP.Console.Parser;

public class TwitchParser: IParser
{
    private HttpClient _client;
    private Stopwatch _watch;

    public TwitchParser()
    {
        _client = new();
        _watch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
    
    public const string clientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";

    private const string aid = "jak3o1u0o97f0kp0xblofxz25ofd7b";
    private const string secret = "hdu1q076y9u0j89e6ferde4d5kxa42";

    private string bearerToken;
    private double expire = -1;

    public async UniTask<JToken> GetAccessToken(string id, bool isVOD, CancellationToken token)
    {
        JObject data = new()
        {
            ["operationName"] = "PlaybackAccessToken",
            ["extensions"] = new JObject
            {
                ["persistedQuery"] = new JObject
                {
                    ["version"] = 1,
                    ["sha256Hash"] = "0828119ded1c13477966434e15800ff57ddacf13ba1911c129dc2200705b0712"
                }
            },
            ["variables"] = new JObject
            {
                ["isLive"] = !isVOD,
                ["login"] = isVOD ? "" : id,
                ["isVod"] = isVOD,
                ["vodID"] = isVOD ? id : "",
                ["playerType"] = "embed"
            }
        };

        HttpRequestMessage options = new(HttpMethod.Post, "https://gql.twitch.tv/gql");
        options.Headers.Add("Client-id", clientId);
        var dataStr = data.ToString(Newtonsoft.Json.Formatting.None);
        StringContent content = new(dataStr, Encoding.UTF8);
        options.Content = content;
        var req = await _client.SendAsync(options, token);
        req.EnsureSuccessStatusCode();
        var res = await req.Content.ReadAsStringCompressAsync(token);
        var resData = JObject.Parse(res);
        if (isVOD)
        {
            return resData["data"]["videoPlaybackAccessToken"];
        }
        return resData["data"]["streamPlaybackAccessToken"];
    }
    
    public bool ShallIntercept(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.Host;

        return host.Equals("twitch.tv", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("www.twitch.tv", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("m.twitch.tv", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("embed.twitch.tv", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsValid(string url)
    {
        if (!ShallIntercept(url)) return false;
        Uri uri = new(url, UriKind.Absolute);

        if (Regex.IsMatch(uri.AbsolutePath, @"^\/videos(?!\/[0-9]+$)(?:\/.*)?$")) return false;
        if (Regex.IsMatch(uri.AbsolutePath, @"^\/videos\/[0-9]+\/?$")) return true;
        return Regex.IsMatch(uri.AbsolutePath, @"^\/[a-zA-Z0-9_]+(?:\/.*)?$");
    }

    public async UniTask<string[]> Parse(string url, CancellationToken token)
    {
        Uri uri = new(url, UriKind.Absolute);
        bool isVOD;
        string id;
        if (Regex.Match(uri.AbsolutePath, @"^\/videos\/([0-9]+)\/?$") is { Success: true } matchVideo)
        {
            isVOD = true;
            id = matchVideo.Groups[1].Value;
        } else if (Regex.Match(uri.AbsolutePath, @"^\/([a-zA-Z0-9_]+)(?:\/.*)?$") is { Success: true } matchStream)
        {
            isVOD = false;
            id = matchStream.Groups[1].Value;
        }
        else
        {
            throw new ArgumentException("Invalid path. How did you get here?");
        }

        var at = await GetAccessToken(id, isVOD, token);
        return new[]
        {
            $"https://usher.ttvnw.net/{(isVOD ? "vod" : "api/channel/hls")}/{id}.m3u8?client_id={clientId}&token={at["value"].Value<string>()}&sig={at["signature"].Value<string>()}&allow_source=true",
            ""
        };
    }

    public async UniTask<string[]> Info(string url, CancellationToken token)
    {
        Uri uri = new(url, UriKind.Absolute);
        bool isVOD;
        string id;
        if (Regex.Match(uri.AbsolutePath, @"^\/videos\/([0-9]+)\/?$") is { Success: true } matchVideo)
        {
            isVOD = true;
            id = matchVideo.Groups[1].Value;
        }
        else if (Regex.Match(uri.AbsolutePath, @"^\/([a-zA-Z0-9_]+)(?:\/.*)?$") is { Success: true } matchStream)
        {
            isVOD = false;
            id = matchStream.Groups[1].Value;
        }
        else
        {
            throw new ArgumentException("Invalid path. How did you get here?");
        }


        if (expire <= _watch.Elapsed.TotalSeconds + 300)
        {
            FormUrlEncodedContent tokenContent = new(new Dictionary<string, string>
            {
                {"client_id", aid},
                {"client_secret", secret},
                {"grant_type", "client_credentials"}
            });
            var time = _watch.Elapsed.TotalSeconds;
            var resp = await _client.PostAsync("https://id.twitch.tv/oauth2/token", tokenContent, token);
            resp.EnsureSuccessStatusCode();
            var respObj = JObject.Parse(await resp.Content.ReadAsStringCompressAsync(token));
            expire = time + respObj["expires_in"].Value<double>();
            bearerToken = respObj["access_token"].Value<string>();
        }

        HttpRequestMessage message;
        
        if (isVOD)
        {
            message = new(HttpMethod.Get, $"https://api.twitch.tv/helix/videos?id={id}");
        }
        else
        {
            message = new(HttpMethod.Get, $"https://api.twitch.tv/helix/streams?user_login={Uri.EscapeDataString(id)}");
        }
        message.Headers.Add("Authorization", $"Bearer {bearerToken}");
        message.Headers.Add("Client-Id", aid);
        var vodResp = await _client.SendAsync(message, token);
        vodResp.EnsureSuccessStatusCode();
        var parseObj = JObject.Parse(await vodResp.Content.ReadAsStringCompressAsync(token));
        return new[]
        {
            parseObj["data"][0]["title"].Value<string>() ?? "",
            parseObj["data"][0]["user_name"].Value<string>() ?? "",
            parseObj["data"][0]["thumbnail_url"].Value<string>() ?? ""
        };
    }
}