using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DuckovVP.Console.Parser.Bili;

public class BiliVideo
{
    protected string _bv;
    protected ulong _av;
    protected BiliApi _api { get; }
    private JObject? _info;
    
    public BiliVideo(BiliApi api, string bv, ulong av)
    {
        _bv = bv;
        _av = av;
        _api = api;
        _info = null;
    }

    public BiliVideo(BiliApi api, ulong av): this(api, BiliUtils.Aid2Bid(av), av)
    {
    }

    public BiliVideo(BiliApi api, string bv) : this(api, bv, BiliUtils.Bid2Aid(bv))
    {
    }

    public virtual async UniTask<JObject> GetInfo(CancellationToken token = default)
    {
        var api = _api.MakeApi("https://api.bilibili.com/x/web-interface/view", "GET");
        api.verify = false;
        api.param = new()
        {
            { "aid", _av },
            { "bvid", _bv }
        };
        JObject resp = (JObject) await api.Request(false, false, token);
        _info = resp;
        return resp;
    }

    public async UniTask<ulong> GetCidByIndex(int page_index, CancellationToken token)
    {
        if (page_index < 0) throw new ArgumentException("Page index must > 0");
        var info = _info ?? await GetInfo(token);
        var pages = info["pages"] as JArray;
        if (pages.Count <= page_index)
        {
            throw new ArgumentException("No such page exist.");
        }

        var page = pages[page_index];
        return page["cid"].Value<ulong>();
    }

    public virtual async UniTask<(string?, string?)> GetDownloadUrl(int page_index, CancellationToken token = default)
    {
        var cid = await GetCidByIndex(page_index, token);
        var api = _api.MakeApi("https://api.bilibili.com/x/player/wbi/playurl", "GET");
        api.verify = false;
        api.param = new()
        {
            { "qn", "127" },
            { "fnval", "4048" },
            { "fnver", "0" },
            { "fourk", "1" },
            { "gaia_source", "pre-load" },
            { "isGaiaAvoided", "true" },
            { "avid", _av.ToString() },
            { "bvid", _bv },
            { "cid", cid.ToString() },
            { "from_client", "BROWSER" },
            { "web_location", "1315873" },
            // { "platform", "html5" },
            // { "high_quality", "1" }
        };
        api.wbi = true;
        var results = (JToken) await api.Request(false, false, token);
        if (results["video_info"] != null)
        {
            results = results["video_info"];
        }
        
        if (results["durl"] != null)
        {
            return (results["durl"][0]["url"].Value<string>(), null);
        }

        var dash = results["dash"];
        var videoList = (JArray)dash["video"];
        var video = videoList
            .Where(s => s["baseUrl"] != null)
            .OrderBy(s => s["width"].Value<int>() >= 480 ? 0 : 1)
            .ThenBy(s => (s["width"].Value<int>() >= 480 ? 1 : -1) * s["width"].Value<int>())
            .ThenBy(s =>
            {
                var code = s["codecs"].Value<string>().ToLowerInvariant();
                if (code.Contains("av01") || code.Contains("av1")) return 0;
                if (code.Contains("hev") || code.Contains("hvc")) return 1;
                if (code.Contains("avc")) return 2;
                return 3;
            })
            .First()["baseUrl"].Value<string>();
        string? audio = null;
        if (dash["audio"] is JArray audioList)
        {
            audio = audioList
                .Where(s => s["baseUrl"] != null)
                .Where(s => s["id"].Value<int>() <= 30232)
                .OrderByDescending(s => s["id"].Value<int>())
                .First()["baseUrl"].Value<string>();
        }

        return (video, audio);
    }
}