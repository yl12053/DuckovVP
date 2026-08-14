using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DuckovVP.Console.Parser.Bili;

public class BiliAudio
{
    private BiliApi _api;
    private ulong au;
    public BiliAudio(BiliApi api, ulong au)
    {
        this._api = api;
        this.au = au;
    }

    public async UniTask<JToken> GetInfo(CancellationToken token = default)
    {
        var api = _api.MakeApi("https://www.bilibili.com/audio/music-service-c/web/song/info", "GET");
        api.verify = false;
        api.param = new()
        {
            { "sid", au.ToString() }
        };
        return (JToken) await api.Request(false, false, token);
    }

    public async UniTask<string> GetDownloadUrl(CancellationToken token = default)
    {
        var api = _api.MakeApi("https://www.bilibili.com/audio/music-service-c/web/url", "GET");
        api.verify = false;
        api.param = new()
        {
            { "sid", au.ToString() },
            { "privilege", "2" },
            { "quality", "2" }
        };
        var ret = (JToken)await api.Request(false, false, token);
        return ret["cdns"][0].Value<string>();
    }
}