using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DuckovVP.Console.Parser;

public class YoutubeParser: IParser
{
    private YoutubeClient youtube = new();

    public void Dispose()
    {
        youtube.Dispose();
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

        return host.EndsWith("youtube.com", StringComparison.OrdinalIgnoreCase) || host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsValid(string url)
    {
        if (!ShallIntercept(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            var abs = uri.AbsolutePath;
            return Regex.IsMatch(abs, @"^/[a-zA-Z0-9_-]{11}");
        }

        if (!uri.AbsolutePath.Equals("/watch"))
        {
            return false;
        }

        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        var v = queryParams["v"];
        if (string.IsNullOrEmpty(v)) return false;
        return Regex.IsMatch(v, @"^[a-zA-Z0-9_-]{11}");
    }

    public async UniTask<string[]> Info(string url, CancellationToken token)
    {
        var infos = await youtube.Videos.GetAsync(url, token);
        return new[]
        {
            infos.Title,
            infos.Author.ChannelTitle,
            infos.Thumbnails
                .OrderByDescending(t => t.Resolution.Area)
                .First()
                ?.Url ?? ""
        };
    }

    public async UniTask<string[]> Parse(string url, CancellationToken token)
    {
        var stream = await youtube.Videos.Streams.GetManifestAsync(url, token);
        var video = stream.GetVideoOnlyStreams()
            .OrderBy(s => s.VideoResolution.Width >= 480 ? 0 : 1)
            .ThenBy(s => (s.VideoResolution.Width >= 480 ? 1 : -1) * s.VideoResolution.Width)
            .FirstOrDefault();
        var audio = stream.GetAudioOnlyStreams().GetWithHighestBitrate();
        return new[] { video?.Url ?? "", audio.Url};
    }
}