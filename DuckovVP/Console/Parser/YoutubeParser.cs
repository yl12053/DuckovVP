using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YoutubeExplode;
using YoutubeExplode.Bridge;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos;
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
        return VideoId.TryParse(url) != null;
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
        var streamClient = youtube.Videos.Streams;
        VideoId vid = url;
        PlayerResponse playerResponse;
        for (var retriesRemaining = 5; ; retriesRemaining--)
        {
            try
            {
                playerResponse = await streamClient._controller.GetPlayerResponseAsync(vid, token, true);
                break;
            }
            catch (Exception ex)
                when (ex is HttpRequestException or IOException && retriesRemaining > 0) { }
        }
        if (!playerResponse.IsPlayable)
        {
            throw new VideoUnplayableException(
                $"Video '{vid}' is unplayable. Reason: '{playerResponse.PlayabilityError}'."
            );
        }

        if (!string.IsNullOrWhiteSpace(playerResponse.HlsManifestUrl))
        {
            return new[] { playerResponse.HlsManifestUrl, "" };
        }

        if (!playerResponse.IsAvailable)
        {
            throw new VideoUnavailableException($"Video '{vid}' is not available.");
        }
        
        StreamManifest manifest;
        for (var retriesRemaining = 5; ; retriesRemaining--)
        {
            try
            {
                try
                {
                    var infos = await streamClient.GetStreamInfosAsync(vid, playerResponse, token);
                    manifest = new(infos);
                    break;
                }
                catch (VideoUnplayableException ex) when (ex is not VideoUnavailableException)
                {
                    var cipherManifest = await streamClient.ResolveCipherManifestAsync(token);
                    
                    var playerResponse2 = await streamClient._controller.GetPlayerResponseAsync(
                        vid,
                        cipherManifest.SignatureTimestamp,
                        token
                    );

                    var infos = await streamClient.GetStreamInfosAsync(vid, playerResponse, token);
                    manifest = new(infos);
                }
            }
            catch (Exception ex)
                when (ex is HttpRequestException or IOException && retriesRemaining > 0) { }
        }
        var video = manifest.GetVideoOnlyStreams()
            .OrderBy(s => s.VideoResolution.Width >= 480 ? 0 : 1)
            .ThenBy(s => (s.VideoResolution.Width >= 480 ? 1 : -1) * s.VideoResolution.Width)
            .FirstOrDefault();
        var audio = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        return new[] { video?.Url ?? "", audio.Url};
    }
}