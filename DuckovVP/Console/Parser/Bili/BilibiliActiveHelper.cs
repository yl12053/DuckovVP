using System.Numerics;
using UnityEngine;

namespace DuckovVP.Console.Parser.Bili;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class BilibiliActiveHelper
{
    private const ulong MOD = 1UL << 64;

    public static async UniTask<JObject> ActiveBuvidAsync(HttpClient client, string url, string buvid3, string buvid4, Dictionary<string, string> baseHeaders, CancellationToken cancellationToken = default)
    {
        string uuid = GenUuidInfoc();
        string payload = GetPayload(uuid);
        string buvidFp = GenBuvidFp(payload, 31);
        
        var headers = new Dictionary<string, string>(baseHeaders)
        {
            ["Content-Type"] = "application/json"
        };
        
        var cookies = new Dictionary<string, string>
        {
            ["buvid3"] = buvid3,
            ["buvid4"] = buvid4,
            ["buvid_fp"] = buvidFp,
            ["_uuid"] = uuid
        };

        string cookieString = string.Join("; ", cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        foreach (var header in headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        request.Headers.Add("Cookie", cookieString);
        
        HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        string responseString = await response.Content.ReadAsStringAsync().AsUniTaskCpy().AttachExternalCancellation(cancellationToken);
        JObject data = JObject.Parse(responseString);

        int code = data["code"]?.Value<int>() ?? -1;
        if (code != 0)
        {
            string msg = data["msg"]?.ToString() ?? "Unknown error";
            throw new Exception($"ExClimbWuzhiException: [{code}] {msg}");
        }

        return data;
    }

    private static long GetTimeMilli()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private static ulong RotateLeft(ulong x, int k)
    {
        k %= 64;
        return (x << k) | (x >> (64 - k));
    }

    private static string GenUuidInfoc()
    {
        long t = GetTimeMilli() % 100000;
        string[] mp = {"1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "10"};
        int[] pck = { 8, 4, 4, 4, 12 };
        
        var rand = new Random();
        var parts = new List<string>();
        foreach (int len in pck)
        {
            var sb = new StringBuilder(len);
            for (int i = 0; i < len; i++)
            {
                sb.Append(mp[rand.Next(mp.Length)]);
            }
            parts.Add(sb.ToString());
        }

        string uuidPart = string.Join("-", parts);
        string tStr = t.ToString().PadRight(5, '0');
        return $"{uuidPart}-{tStr}infoc";
    }

    private static string GenBuvidFp(string key, int seed)
    {
        byte[] keyBytes = Encoding.ASCII.GetBytes(key);
        ulong low;
        ulong high;
        (high, low) = Murmur3X64128(keyBytes, (uint)seed);
        
        return $"{low:x16}{high:x16}";
    }

    private static (ulong, ulong) Murmur3X64128(byte[] source, uint seed)
    {
        ulong C1 = 0x87C37B91114253D5UL;
        ulong C2 = 0x4CF5AD432745937FUL;
        ulong C3 = 0x52DCE729UL;
        ulong C4 = 0x38495AB5UL;
        int R1 = 27, R2 = 31, R3 = 33;
        ulong M = 5;
        ulong h1 = seed, h2 = seed;
        int processed = 0;
        int offset = 0;

        while (true)
        {
            int remaining = source.Length - offset;
            if (remaining >= 16)
            {
                ulong k1 = (ulong) BitConverter.ToInt64(source, offset);
                ulong k2 = (ulong) BitConverter.ToInt64(source, offset + 8);
                offset += 16;
                processed += 16;

                h1 ^= RotateLeft(k1 * C1, R2) * C2;
                h1 = (RotateLeft(h1, R1) + h2) * M + C3;
                h2 ^= RotateLeft(k2 * C2, R3) * C1;
                h2 = (RotateLeft(h2, R2) + h1) * M + C4;
            }
            else if (remaining == 0)
            {
                h1 ^= (ulong)processed;
                h2 ^= (ulong)processed;
                h1 += h2;
                h2 += h1;
                h1 = Fmix64(h1);
                h2 = Fmix64(h2);
                h1 += h2;
                h2 += h1;
                
                return (h2, h1);
            }
            else
            {
                ulong k1 = 0;
                ulong k2 = 0;
                processed += remaining;

                if (remaining >= 15) k2 ^= (ulong)source[offset + 14] << 48;
                if (remaining >= 14) k2 ^= (ulong)source[offset + 13] << 40;
                if (remaining >= 13) k2 ^= (ulong)source[offset + 12] << 32;
                if (remaining >= 12) k2 ^= (ulong)source[offset + 11] << 24;
                if (remaining >= 11) k2 ^= (ulong)source[offset + 10] << 16;
                if (remaining >= 10) k2 ^= (ulong)source[offset + 9] << 8;
                if (remaining >= 9)
                {
                    k2 ^= source[offset + 8];
                    k2 = RotateLeft(k2 * C2, R3) * C1;
                    h2 ^= k2;
                }
                if (remaining >= 8) k1 ^= (ulong)source[offset + 7] << 56;
                if (remaining >= 7) k1 ^= (ulong)source[offset + 6] << 48;
                if (remaining >= 6) k1 ^= (ulong)source[offset + 5] << 40;
                if (remaining >= 5) k1 ^= (ulong)source[offset + 4] << 32;
                if (remaining >= 4) k1 ^= (ulong)source[offset + 3] << 24;
                if (remaining >= 3) k1 ^= (ulong)source[offset + 2] << 16;
                if (remaining >= 2) k1 ^= (ulong)source[offset + 1] << 8;
                if (remaining >= 1) k1 ^= source[offset];

                k1 = RotateLeft(k1 * C1, R2) * C2;
                h1 ^= k1;

                offset += remaining;
            }
        }
    }

    private static ulong Fmix64(ulong k)
    {
        k ^= k >> 33;
        k *= 0xFF51AFD7ED558CCDUL;
        k ^= k >> 33;
        k *= 0xC4CEB9FE1A85EC53UL;
        k ^= k >> 33;
        return k;
    }

    private static string GetPayload(string uuid)
    {
        var content = new Dictionary<string, object?>
        {
            ["3064"] = 1,
            ["5062"] = GetTimeMilli(),
            ["03bf"] = "https%3A%2F%2Fwww.bilibili.com%2F",
            ["39c8"] = "333.788.fp.risk",
            ["34f1"] = "",
            ["d402"] = "",
            ["654a"] = "",
            ["6e7c"] = "839x959",
            ["3c43"] = new Dictionary<string, object?>
            {
                ["2673"] = 0,
                ["5766"] = 24,
                ["6527"] = 0,
                ["7003"] = 1,
                ["807e"] = 1,
                ["b8ce"] = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.3 Safari/605.1.15",
                ["641c"] = 0,
                ["07a4"] = "en-US",
                ["1c57"] = "not available",
                ["0bd0"] = 8,
                ["748e"] = new int[] { 900, 1440 },
                ["d61f"] = new int[] { 875, 1440 },
                ["fc9d"] = -480,
                ["6aa9"] = "Asia/Shanghai",
                ["75b8"] = 1,
                ["3b21"] = 1,
                ["8a1c"] = 0,
                ["d52f"] = "not available",
                ["adca"] = "MacIntel",
                ["80c9"] = new object[]
                {
                    new object[] { "PDF Viewer", "Portable Document Format", new object[] { new string[] { "application/pdf", "pdf" }, new string[] { "text/pdf", "pdf" } } },
                    new object[] { "Chrome PDF Viewer", "Portable Document Format", new object[] { new string[] { "application/pdf", "pdf" }, new string[] { "text/pdf", "pdf" } } },
                    new object[] { "Chromium PDF Viewer", "Portable Document Format", new object[] { new string[] { "application/pdf", "pdf" }, new string[] { "text/pdf", "pdf" } } },
                    new object[] { "Microsoft Edge PDF Viewer", "Portable Document Format", new object[] { new string[] { "application/pdf", "pdf" }, new string[] { "text/pdf", "pdf" } } },
                    new object[] { "WebKit built-in PDF", "Portable Document Format", new object[] { new string[] { "application/pdf", "pdf" }, new string[] { "text/pdf", "pdf" } } }
                },
                ["13ab"] = "0dAAAAAASUVORK5CYII=",
                ["bfe9"] = "QgAAEIQAACEIAABCCQN4FXANGq7S8KTZayAAAAAElFTkSuQmCC",
                ["a3c1"] = new string[]
                {
                    "extensions:ANGLE_instanced_arrays;EXT_blend_minmax;...", // 略写，保持和Python一致
                    "webgl aliased line width range:[1, 1]",
                    "webgl aliased point size range:[1, 511]",
                    "webgl alpha bits:8",
                    "webgl antialiasing:yes",
                    "webgl blue bits:8",
                    "webgl depth bits:24",
                    "webgl green bits:8",
                    "webgl max anisotropy:16",
                    "webgl max combined texture image units:32",
                    "webgl max cube map texture size:16384",
                    "webgl max fragment uniform vectors:1024",
                    "webgl max render buffer size:16384",
                    "webgl max texture image units:16",
                    "webgl max texture size:16384",
                    "webgl max varying vectors:30",
                    "webgl max vertex attribs:16",
                    "webgl max vertex texture image units:16",
                    "webgl max vertex uniform vectors:1024",
                    "webgl max viewport dims:[16384, 16384]",
                    "webgl red bits:8",
                    "webgl renderer:WebKit WebGL",
                    "webgl shading language version:WebGL GLSL ES 1.0 (1.0)",
                    "webgl stencil bits:0",
                    "webgl vendor:WebKit",
                    "webgl version:WebGL 1.0",
                    "webgl unmasked vendor:Apple Inc.",
                    "webgl unmasked renderer:Apple GPU",
                    "webgl vertex shader high float precision:23",
                    "webgl vertex shader high float precision rangeMin:127",
                    "webgl vertex shader high float precision rangeMax:127",
                    "webgl vertex shader medium float precision:23",
                    "webgl vertex shader medium float precision rangeMin:127",
                    "webgl vertex shader medium float precision rangeMax:127",
                    "webgl vertex shader low float precision:23",
                    "webgl vertex shader low float precision rangeMin:127",
                    "webgl vertex shader low float precision rangeMax:127",
                    "webgl fragment shader high float precision:23",
                    "webgl fragment shader high float precision rangeMin:127",
                    "webgl fragment shader high float precision rangeMax:127",
                    "webgl fragment shader medium float precision:23",
                    "webgl fragment shader medium float precision rangeMin:127",
                    "webgl fragment shader medium float precision rangeMax:127",
                    "webgl fragment shader low float precision:23",
                    "webgl fragment shader low float precision rangeMin:127",
                    "webgl fragment shader low float precision rangeMax:127",
                    "webgl vertex shader high int precision:0",
                    "webgl vertex shader high int precision rangeMin:31",
                    "webgl vertex shader high int precision rangeMax:30",
                    "webgl vertex shader medium int precision:0",
                    "webgl vertex shader medium int precision rangeMin:31",
                    "webgl vertex shader medium int precision rangeMax:30",
                    "webgl vertex shader low int precision:0",
                    "webgl vertex shader low int precision rangeMin:31",
                    "webgl vertex shader low int precision rangeMax:30",
                    "webgl fragment shader high int precision:0",
                    "webgl fragment shader high int precision rangeMin:31",
                    "webgl fragment shader high int precision rangeMax:30",
                    "webgl fragment shader medium int precision:0",
                    "webgl fragment shader medium int precision rangeMin:31",
                    "webgl fragment shader medium int precision rangeMax:30",
                    "webgl fragment shader low int precision:0",
                    "webgl fragment shader low int precision rangeMin:31",
                    "webgl fragment shader low int precision rangeMax:30"
                },
                ["6bc5"] = "Apple Inc.~Apple GPU",
                ["ed31"] = 0,
                ["72bd"] = 0,
                ["097b"] = 0,
                ["52cd"] = new int[] { 0, 0, 0 },
                ["a658"] = new string[]
                {
                    "Andale Mono", "Arial", "Arial Black", "Arial Hebrew", "Arial Narrow",
                    "Arial Rounded MT Bold", "Arial Unicode MS", "Comic Sans MS", "Courier",
                    "Courier New", "Geneva", "Georgia", "Helvetica", "Helvetica Neue", "Impact",
                    "LUCIDA GRANDE", "Microsoft Sans Serif", "Monaco", "Palatino", "Tahoma",
                    "Times", "Times New Roman", "Trebuchet MS", "Verdana", "Wingdings",
                    "Wingdings 2", "Wingdings 3"
                },
                ["d02f"] = "124.04345259929687"
            },
            ["54ef"] = "{\"in_new_ab\":true,\"ab_version\":{\"remove_back_version\":\"REMOVE\",\"login_dialog_version\":\"V_PLAYER_PLAY_TOAST\",\"open_recommend_blank\":\"SELF\",\"storage_back_btn\":\"HIDE\",\"call_pc_app\":\"FORBID\",\"clean_version_old\":\"GO_NEW\",\"optimize_fmp_version\":\"LOADED_METADATA\",\"for_ai_home_version\":\"V_OTHER\",\"bmg_fallback_version\":\"DEFAULT\",\"ai_summary_version\":\"SHOW\",\"weixin_popup_block\":\"ENABLE\",\"rcmd_tab_version\":\"DISABLE\",\"in_new_ab\":true},\"ab_split_num\":{\"remove_back_version\":11,\"login_dialog_version\":43,\"open_recommend_blank\":90,\"storage_back_btn\":87,\"call_pc_app\":47,\"clean_version_old\":46,\"optimize_fmp_version\":28,\"for_ai_home_version\":38,\"bmg_fallback_version\":86,\"ai_summary_version\":466,\"weixin_popup_block\":45,\"rcmd_tab_version\":90,\"in_new_ab\":0},\"pageVersion\":\"new_video\",\"videoGoOldVersion\":-1}",
            ["8b94"] = "https%3A%2F%2Fwww.bilibili.com%2F",
            ["df35"] = uuid,
            ["07a4"] = "en-US",
            ["5f45"] = null,
            ["db46"] = 0
        };
        
        var jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
        {
            StringEscapeHandling = Newtonsoft.Json.StringEscapeHandling.Default,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include
        };

        string contentJson = Newtonsoft.Json.JsonConvert.SerializeObject(content, Newtonsoft.Json.Formatting.None, jsonSettings);
        
        var wrapper = new Dictionary<string, string>
        {
            ["payload"] = contentJson
        };

        return Newtonsoft.Json.JsonConvert.SerializeObject(wrapper, Newtonsoft.Json.Formatting.None, jsonSettings);
    }
}