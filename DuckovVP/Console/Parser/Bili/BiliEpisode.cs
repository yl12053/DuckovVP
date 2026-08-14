using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DuckovVP.Console.Parser.Bili;

public class BiliEpisode: BiliVideo
{
    private ulong ep;
    private JToken? playUrl = null;

    public BiliEpisode(BiliApi api, ulong ep) : base(api, null, 0)
    {
        this.ep = ep;
    }

    private async UniTask<JToken> GetDownloadUrlI(CancellationToken token)
    {
        if (playUrl == null)
        {
            var api = _api.MakeApi("https://api.bilibili.com/pgc/player/web/v2/playurl", "GET");
            api.verify = false;
            api.param = new()
            {
                {"ep_id", ep.ToString()},
                {"qn", "127"},
                {"otype", "json"},
                {"fnval", "4048"},
                {"fourk", "1"},
                {"gaia_source", ""},
                {"from_client", "BROWSER"},
                {"is_main_page", "false"},
                {"need_fragment", "false"},
                {"isGaiaAvoided", "true"},
                {"web_location", "1315873"}
            };
            playUrl = (JToken) await api.Request(false, false, token);
        }

        return playUrl;
    }

    public override async UniTask<JObject> GetInfo(CancellationToken token = default)
    {
        var resp = await GetDownloadUrlI(token);
        var content = resp["play_view_business_info"];
        _bv = content["episode_info"]["bvid"].Value<string>();
        _av = BiliUtils.Bid2Aid(_bv);
        return await base.GetInfo(token);
    }

    public override async UniTask<(string?, string?)> GetDownloadUrl(int page_index, CancellationToken token = default)
    {
        var resp = await GetDownloadUrlI(token);
        var content = resp["play_view_business_info"];
        _bv = content["episode_info"]["bvid"].Value<string>();
        _av = BiliUtils.Bid2Aid(_bv);
        return await base.GetDownloadUrl(page_index, token);
    }
}