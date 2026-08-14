using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Cysharp.Threading.Tasks;
using DuckovVP.Console.Parser.Bili;
using LibVLCSharp.Shared;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json.Linq;

namespace DuckovVP.Console.Parser;

public class BiliParser: IParser
{
    private BiliApi _client;

    public void Dispose()
    {
        _client.Dispose();
    }

    public BiliParser()
    {
        _client = new();
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

        return host.Equals("www.bilibili.com", StringComparison.OrdinalIgnoreCase)
               || host.Equals("m.bilibili.com", StringComparison.OrdinalIgnoreCase)
               || host.Equals("bilibili.com", StringComparison.OrdinalIgnoreCase)
               || host.Equals("live.bilibili.com", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsValid(string url)
    {
        if (!ShallIntercept(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (uri.Host.Equals("live.bilibili.com", StringComparison.OrdinalIgnoreCase))
        {
            return Regex.IsMatch(uri.AbsolutePath, @"^\/[0-9]+\/?$");
        }
        
        if (
            Regex.IsMatch(uri.AbsolutePath, @"^\/bangumi\/play\/ep[0-9]+\/?$")
            || Regex.IsMatch(uri.AbsolutePath, @"^\/audio\/au[0-9]+\/?$")
        ) return true;
        
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        var p = queryParams["p"];
        if (!string.IsNullOrWhiteSpace(p))
        {
            if (!int.TryParse(p, out var pi)) return false;
            if (pi < 1) return false;
        }
        return
            Regex.IsMatch(uri.AbsolutePath, @"^\/video\/BV[a-zA-Z0-9]{10}\/?$")
            || Regex.IsMatch(uri.AbsolutePath, @"^\/video\/av[0-9]+\/?$");
    }

    public async UniTask<string[]> Parse(string url, CancellationToken token)
    {
        Uri uri = new(url, UriKind.Absolute);
        if (uri.Host.Equals("live.bilibili.com", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(uri.AbsolutePath, @"^\/([0-9]+)\/?$");
            if (!match.Success) return new[] { "", "" };
            BiliLive live = new(_client, ulong.Parse(match.Groups[1].Value));
            return new[] { await live.GetLiveURL(token) ?? "", "" };
        }
        if (Regex.Match(uri.AbsolutePath, @"^\/audio\/au([0-9]+)\/?$") is { Success: true } matchAu)
        {
            BiliAudio audio = new(_client, ulong.Parse(matchAu.Groups[1].Value));
            return new[] { await audio.GetDownloadUrl(token) ?? "", "" };
        }
        BiliVideo video;
        bool parseP = true;
        if (Regex.Match(uri.AbsolutePath, @"^\/video\/(BV[a-zA-Z0-9]{10})\/?$") is { Success: true } matchBv)
        {
            video = new(_client, matchBv.Groups[1].Value);
        }
        else if (Regex.Match(uri.AbsolutePath, @"^\/video\/av([0-9]+)\/?$") is { Success: true } matchAv)
        {
            video = new(_client, ulong.Parse(matchAv.Groups[1].Value));
        }
        else if (Regex.Match(uri.AbsolutePath, @"^\/bangumi\/play\/ep([0-9]+)\/?$") is {Success: true} matchEp)
        {
            video = new BiliEpisode(_client, ulong.Parse(matchEp.Groups[1].Value));
            parseP = false;
        }
        else
        {
            throw new ArgumentException("Unexpected branch");
        }

        string? p = null;
        if (parseP)
        {
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            p = queryParams["p"];
        }
        var result = await video.GetDownloadUrl(string.IsNullOrWhiteSpace(p) ? 0 : int.Parse(p) - 1, token);
        return new[] {result.Item1 ?? "", result.Item2 ?? ""};
    }

    public async UniTask<string[]> Info(string url, CancellationToken token)
    {
        Uri uri = new(url, UriKind.Absolute);
        if (uri.Host.Equals("live.bilibili.com", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(uri.AbsolutePath, @"^\/([0-9]+)\/?$");
            if (!match.Success) return new[] { "", "" };
            BiliLive live = new(_client, ulong.Parse(match.Groups[1].Value));
            var res = await live.GetInfo(token);
            return new[] { res.Item1 ?? "Unnamed CD", res.Item2 ?? "Unknown Artist", res.Item3 ?? "" };
        }
        if (Regex.Match(uri.AbsolutePath, @"^\/audio\/au([0-9]+)\/?$") is { Success: true } matchAu)
        {
            BiliAudio audio = new(_client, ulong.Parse(matchAu.Groups[1].Value));
            var infoa = await audio.GetInfo(token);
            return new[]
            {
                infoa["title"]?.Value<string>() ?? "Unnamed CD", 
                infoa["author"]?.Value<string>() ?? "Unknown Artist",
                infoa["cover"]?.Value<string>() ?? ""
            };
        }
        BiliVideo video;
        if (Regex.Match(uri.AbsolutePath, @"^\/video\/(BV[a-zA-Z0-9]{10})\/?$") is { Success: true } matchBv)
        {
            video = new(_client, matchBv.Groups[1].Value);
        }
        else if (Regex.Match(uri.AbsolutePath, @"^\/video\/av([0-9]+)\/?$") is { Success: true } matchAv)
        {
            video = new(_client, ulong.Parse(matchAv.Groups[1].Value));
        }
        else if (Regex.Match(uri.AbsolutePath, @"^\/bangumi\/play\/ep([0-9]+)\/?$") is {Success: true} matchEp)
        {
            video = new BiliEpisode(_client, ulong.Parse(matchEp.Groups[1].Value));
        }
        else
        {
            throw new ArgumentException("Unexpected branch");
        }

        var info = await video.GetInfo(token);
        return new[]
        {
            info["title"]?.Value<string>() ?? "Unnamed CD",
            info["owner"]?["name"]?.Value<string>() ?? "Unknown Artist",
            info["pic"]?.Value<string>()
        };
    }

    public void OnMediaCreate(Media media, string[] urls, string original)
    {
        media.AddOption(":http-user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0");
        media.AddOption(":http-referrer=https://www.bilibili.com");
        Uri uri = new(original, UriKind.Absolute);
        if (!Regex.IsMatch(uri.AbsolutePath, @"^\/audio\/au[0-9]+\/?$"))
        {
            media.AddOption(":audio-desync=200");
        }
    }
}