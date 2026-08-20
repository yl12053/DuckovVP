using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DuckovVP.Console.Parser.Bili;

public class BiliLive
{
    private BiliApi _api;
    private ulong _roomid;

    public BiliLive(BiliApi api, ulong roomid)
    {
        _api = api;
        _roomid = roomid;
    }

    public async UniTask<string> GetLiveURL(CancellationToken token)
    {
        var api = _api.MakeApi("https://api.live.bilibili.com/xlive/web-room/v1/playUrl/playUrl", "GET");
        api.verify = false;
        api.param = new()
        {
            { "cid", _roomid.ToString() },
            { "platform", "web" },
            { "qn", "150" },
            { "https_url_req", "1" },
            { "ptype", "16" }
        };
        var res = (JObject)await api.Request(false, false, token);
        return res["durl"][0]["url"].Value<string>();
    }

    public async UniTask<(string?, string?, string?)> GetInfo(CancellationToken token)
    {
        var api = _api.MakeApi("https://api.live.bilibili.com/xlive/web-room/v1/index/getInfoByRoom", "GET");
        api.verify = false;
        api.param = new()
        {
            { "room_id", _roomid.ToString() }
        };
        var res = (JObject)await api.Request(false, false, token);
        var title = res["room_info"]["title"].Value<string>();
        var cover = res["room_info"]["cover"].Value<string>();
        var author = res["anchor_info"]["base_info"]["uname"].Value<string>();
        return (title, author, cover);
    }
}