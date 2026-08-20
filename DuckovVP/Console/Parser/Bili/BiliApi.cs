using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Cysharp.Threading.Tasks;
using FMOD;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

namespace DuckovVP.Console.Parser.Bili;

public class BiliApi: IDisposable
{
    private HttpClient _client;
    private CancellationTokenSource _cts;

    public CancellationToken NewToken(CancellationToken token)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token);
        return cts.Token;
    }
    
    public BiliApi()
    {
        _client = new();
        _cts = new();
    }

    public void Dispose()
    {
        _client.Dispose();
        _cts.Cancel();
    }

    public static void FillHeader(HttpRequestMessage msg, Dictionary<string, string> headers)
    {
        foreach (var kv in headers)
        {
            msg.Headers.Add(kv.Key, kv.Value);
        }
        msg.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        msg.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        msg.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br", 0.4f));
        msg.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd", 0f));
    }

    public static readonly Dictionary<string, string> Headers = new()
    {
        {
            "User-Agent",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0"
        },
        {
            "Referer",
            "https://www.bilibili.com"
        }
    };

    public static readonly int[] OE =
        new[]
        {
            46,
            47,
            18,
            2,
            53,
            8,
            23,
            32,
            15,
            50,
            10,
            31,
            58,
            3,
            45,
            35,
            27,
            43,
            5,
            49,
            33,
            9,
            42,
            19,
            29,
            28,
            14,
            39,
            12,
            38,
            41,
            13,
            37,
            48,
            7,
            16,
            24,
            55,
            40,
            61,
            26,
            17,
            0,
            1,
            60,
            51,
            30,
            4,
            22,
            25,
            54,
            21,
            56,
            59,
            6,
            63,
            57,
            62,
            11,
            36,
            20,
            34,
            44,
            52
        };

    private async UniTask<JObject> get_nav(CancellationToken token)
    {
        var ct = NewToken(token);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.bilibili.com/x/web-interface/nav");
        FillHeader(request, Headers);
        HttpResponseMessage response = await _client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        
        var resp = await response.Content.ReadAsStringCompressAsync(ct);
        var obj = JObject.Parse(resp);
        return (JObject) obj["data"];
    }

    private async UniTask<string> get_mixin_key(CancellationToken token)
    {
        var ct = NewToken(token);

        JObject data = await get_nav(ct);
        JObject wbi_img = (JObject) data["wbi_img"];

        string split(string key)
        {
            return wbi_img[key].Value<string>().Split("/").Last().Split(".")[0];
        }

        string ae = split("img_url") + split("sub_url");
        string le = string.Concat(
            OE.Select(i => i < ae.Length ? ae[i].ToString() : string.Empty)
        );
        return le[..Math.Min(le.Length, 32)];
    }

    private string? wbi_mixin_key;

    public async UniTask<string> get_wbi_mixin_key(CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(wbi_mixin_key))
        {
            wbi_mixin_key = await get_mixin_key(NewToken(token));
        }

        return wbi_mixin_key;
    }

    private string? buvid3;
    private string? buvid4;

    public async UniTask<JObject> get_spi_buvid(CancellationToken token)
    {
        var ct = NewToken(token);
        
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.bilibili.com/x/frontend/finger/spi");
        FillHeader(request, Headers);
        HttpResponseMessage response = await _client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        
        var resp = await response.Content.ReadAsStringCompressAsync(ct);
        var obj = JObject.Parse(resp);
        return (JObject) obj["data"];
    }

    public async UniTask<(string, string)> get_buvid(CancellationToken token)
    {
        if (buvid3 == null || buvid4 == null)
        {
            var ct = NewToken(token);
            var spi = await get_spi_buvid(ct);
            buvid3 = spi["b_3"].Value<string>();
            buvid4 = spi["b_4"].Value<string>();
            await BilibiliActiveHelper.ActiveBuvidAsync(_client, "https://api.bilibili.com/x/internal/gaia-gateway/ExClimbWuzhi", buvid3, buvid4, Headers, ct);
        }

        return (buvid3, buvid4);
    }

    private string bili_ticket = "";
    private long bili_ticket_expires = 0;

    private async UniTask<string> _get_bili_ticket(Dictionary<string, string> cookie, CancellationToken token)
    {
        var ct = NewToken(token);
        byte[] keyBytes = "XgwSnGZ1p"u8.ToArray();
        byte[] messageBytes = Encoding.UTF8.GetBytes($"ts{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");

        string o;
        
        using (var hmac = new HMACSHA256(keyBytes))
        {
            byte[] hashBytes = hmac.ComputeHash(messageBytes);
            
            o = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        Dictionary<string, string> param = new()
        {
            { "key_id", "ec02" },
            { "hexsign", o },
            { "context[ts]", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
            { "csrf", "" }
        };
        string queryString = await new FormUrlEncodedContent(param).ReadAsStringAsync();
        
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.bilibili.com/bapis/bilibili.api.ticket.v1.Ticket/GenWebTicket?{queryString}");
        string cookieString = string.Join("; ", cookie.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        FillHeader(request, Headers);
        request.Headers.Add("Cookie", cookieString);
        HttpResponseMessage response = await _client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        
        var resp = await response.Content.ReadAsStringCompressAsync(ct);
        var obj = JObject.Parse(resp);
        return obj["data"]["ticket"].Value<string>();
    }
    
    public async UniTask<(string, string)> get_bili_ticket(Dictionary<string, string> cookie, CancellationToken token)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > bili_ticket_expires)
        {
            bili_ticket = "";
            bili_ticket_expires = 0;
        }

        if (bili_ticket == "")
        {
            bili_ticket = await _get_bili_ticket(cookie, token);
            bili_ticket_expires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3 * 86400;
        }

        return (bili_ticket, bili_ticket_expires.ToString());
    }
    
    public static string CalculateMd5(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    public static async UniTask<Dictionary<string, object>> enc_sign(Dictionary<string, object> paramsordata)
    {
        paramsordata["appkey"] = "4409e2ce8ffd12b8";
        paramsordata = paramsordata
            .OrderBy(pair => pair.Key)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var stringDict = paramsordata.ToDictionary(
            pair => pair.Key, 
            pair => pair.Value?.ToString() ?? string.Empty
        );
        string encodedString = await new FormUrlEncodedContent(stringDict).ReadAsStringAsync();
        string sign = CalculateMd5(encodedString + "59b43e04ad6965f34319062b478f83dd");
        paramsordata["sign"] = sign;
        return paramsordata;
    }

    public class ResponseCodeException : Exception
    {
        public readonly int code;
        public readonly string reason;
        public readonly JObject data;
        public ResponseCodeException(int code, string reason, JObject data) : base($"{code}: {reason}: {data}")
        {
            this.code = code;
            this.reason = reason;
            this.data = data;
        }
    }

    public Api MakeApi(string url, string method)
    {
        return new Api(this, url, method);
    }
    
    public class Api
    {
        public string url;
        public string method;
        public bool wbi = false;
        public bool dm = false;
        public bool verify = false;
        public bool no_csrf = false;
        public bool json_body = false;
        public bool ignore_code = false;
        public bool sign = false;
        public Dictionary<string, object> data = new();
        public Dictionary<string, object> param = new();
        public Dictionary<string, string> cookies = new();
        public Dictionary<string, string> headers = new();

        private BiliApi api;

        public Api(
            BiliApi api, 
            string url,
            string method
        )
        {
            this.api = api;
            this.url = url;
            this.method = method.ToUpperInvariant();
        }
        
        private struct Config
        {
            public string method;
            public string url;
            public Dictionary<string, object> param;
            public Dictionary<string, object> data;
            public Dictionary<string, string> cookies;
            public string? jsonData;
            public Dictionary<string, string> headers;
        }
        
        private static Dictionary<string, object> enc_dm(Dictionary<string, object> param)
        {
            Random rand = new Random();
            string dm_rand = "ABCDEFGHIJK";
            param["dm_img_list"] = "[]";
            param["dm_img_str"] = $"{dm_rand[rand.Next(dm_rand.Length)]}{dm_rand[rand.Next(dm_rand.Length)]}";
            param["dm_cover_img_str"] = $"{dm_rand[rand.Next(dm_rand.Length)]}{dm_rand[rand.Next(dm_rand.Length)]}";
            param["dm_img_inter"] = @"{""ds"":[],""wh"":[0,0,0],""of"":[0,0,0]}";
            return param;
        }
        
        public static Dictionary<string, object> enc_wbi(Dictionary<string, object> parameters, string mixinKey)
        {
            parameters.Remove("w_rid");
            
            long wts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            parameters["wts"] = wts;
            
            if (!parameters.ContainsKey("web_location") || parameters["web_location"] == null)
            {
                parameters["web_location"] = 1550101;
            }
            
            var sortedParams = parameters.OrderBy(p => p.Key);
            
            string queryStr = string.Join("&", sortedParams.Select(p => 
                $"{HttpUtility.UrlEncode(p.Key, Encoding.UTF8)}={HttpUtility.UrlEncode(p.Value?.ToString() ?? "", Encoding.UTF8)}"
            ));
            
            string rawStr = queryStr + mixinKey;
            string wRid = CalculateMd5(rawStr);
            
            parameters["w_rid"] = wRid;
            return parameters;
        }
        
        private async UniTask<Config> PrepareRequest(CancellationToken token)
        {
            var ct = api.NewToken(token);
            var new_params = new Dictionary<string, object>();
            var new_data_collection = new Dictionary<string, object>();
            foreach (var kv in param)
            {
                if (kv.Value is bool b)
                {
                    new_params[kv.Key] = b ? 1 : 0;
                } else if (kv.Value != null)
                {
                    new_params[kv.Key] = kv.Value;
                }
            }
            
            foreach (var kv in data)
            {
                if (kv.Value is bool b)
                {
                    new_data_collection[kv.Key] = b ? 1 : 0;
                } else if (kv.Value != null)
                {
                    new_data_collection[kv.Key] = kv.Value;
                }
            }

            param = new_params;
            data = new_data_collection;

            if (verify)
            {
                throw new NotImplementedException("Verify api is not yet supported.");
            }

            if (method != "GET" && !no_csrf)
            {
                throw new NotImplementedException("Api with non-GET and CSRF is not supported yet (as required JCT).");
            }

            if (param.TryGetValue("jsonp", out var raw) && raw?.ToString() == "jsonp")
            {
                param["callback"] = "callback";
            }

            if (dm)
            {
                param = enc_dm(param);
            }
            
            if (wbi)
            {
                param = enc_wbi(param, await api.get_wbi_mixin_key(ct));
            }
            
            // todo: if future add CSRF, make it here
            
            var buvids = await api.get_buvid(ct);
            cookies["buvid3"] = buvids.Item1;
            cookies["buvid4"] = buvids.Item2;
            cookies["opus-goback"] = "1";
            
            (cookies["bili_ticket"], cookies["bili_ticket_expires"]) = await api.get_bili_ticket(cookies, ct);

            if (sign)
            {
                if (method == "POST" || method == "DELETE" || method == "PATCH")
                {
                    data = await enc_sign(data);
                }
                else
                {
                    param = await enc_sign(param);
                }
            }

            Config cfg = new Config()
            {
                method = method,
                url = url,
                param = param,
                data = data,
                cookies = cookies,
                headers = headers.Count == 0 ? new Dictionary<string, string>(Headers) : headers
            };
            
            if (json_body) 
            {
                var jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    StringEscapeHandling = Newtonsoft.Json.StringEscapeHandling.Default,
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Include
                };
                
                cfg.headers["Content-Type"] = "application/json";
                cfg.headers["data"] = Newtonsoft.Json.JsonConvert.SerializeObject(cfg.data, Newtonsoft.Json.Formatting.None, jsonSettings);
            }
            
            return cfg;
        }

        private async UniTask<JToken?> ProcessResponse(HttpResponseMessage resp, bool raw = false, CancellationToken token = default)
        {
            resp.EnsureSuccessStatusCode();
            if ((resp.Content.Headers.ContentLength ?? 0) == 0)
            {
                return null;
            }
            
            // await resp.Content.LoadIntoBufferAsync();
            string resp_text = await resp.Content.ReadAsStringCompressAsync(token);
            JObject resp_data;
            if (param.ContainsKey("callback"))
            {
                Match match = Regex.Match(resp_text, "^.*?({.*}).*$", RegexOptions.Singleline);
                resp_data = JObject.Parse(match.Groups[1].Value);
            }
            else
            {
                try
                {
                    resp_data = JObject.Parse(resp_text);
                }
                catch (JsonReaderException)
                {
                    Debug.Log($"URL: {resp.RequestMessage.RequestUri}");
                    Debug.Log(BitConverter.ToString(await resp.Content.ReadAsByteArrayAsync()));
                    throw;
                }
            }

            if (raw)
            {
                return resp_data;
            }

            var OK = resp_data["OK"];
            if (!ignore_code)
            {
                if (OK == null)
                {
                    var code = resp_data["code"]?.Value<int>();
                    if (code == null)
                    {
                        throw new ResponseCodeException(-1, "No Code is provided in response", resp_data);
                    }

                    if (code != 0)
                    {
                        var msg = resp_data["msg"]?.Value<string>();
                        if (msg == null)
                        {
                            msg = resp_data["message"]?.Value<string>();
                        }

                        if (msg == null)
                        {
                            msg = "No error message provided.";
                        }
                        throw new ResponseCodeException(code ?? -1, msg, resp_data);
                    }
                } else if (OK.Value<int>() != 1)
                {
                    throw new ResponseCodeException(-1, "OK != 1", resp_data);
                }
            }

            JToken? real_data = resp_data;
            if (OK == null)
            {
                real_data = resp_data["data"];
                if (real_data == null)
                {
                    real_data = resp_data["result"];
                }
            }

            return real_data;
        }

        private async UniTask<object?> _Request(bool raw = false, bool bytes = false, CancellationToken token = default)
        {
            var config = await PrepareRequest(token);
            var client = api._client;
            
            string queryString = await new FormUrlEncodedContent(config.param.Select(k => new KeyValuePair<string, string>(k.Key, k.Value?.ToString() ?? ""))).ReadAsStringAsync();
        
            using var request = new HttpRequestMessage(new(config.method), $"{config.url}?{queryString}");
            if (!string.IsNullOrWhiteSpace(config.jsonData))
            {
                request.Content = new StringContent(config.jsonData, Encoding.UTF8, "application/json");
            }
            else if (config.data.Count > 0)
            {
                request.Content = new FormUrlEncodedContent(config.data.Select(k =>
                    new KeyValuePair<string, string>(k.Key, k.Value?.ToString() ?? "")));
            }
            
            FillHeader(request, config.headers);
            string cookieString = string.Join("; ", config.cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            request.Headers.Add("Cookie", cookieString);
            
            var resp = await client.SendAsync(request, token);
            object? ret;
            if (bytes)
            {
                ret = await resp.Content.ReadAsByteArrayCompressAsync(token);
            }
            else
            {
                ret = await ProcessResponse(resp, raw, token);
            }

            return ret;
        }

        public async UniTask<object?> Request(bool raw = false, bool bytes = false, CancellationToken token = default)
        {
            int loop = 3;
            while (loop != 0)
            {
                if (loop != 3)
                {
                    Debug.Log($"Anti spider, trying wbi for {3 - loop}");
                }

                loop--;
                try
                {
                    return await _Request(raw, bytes, token);
                }
                catch (ResponseCodeException e)
                {
                    if (e.code == -403 && wbi)
                    {
                        api.wbi_mixin_key = null;
                        continue;
                    }

                    throw;
                }
            }

            throw new Exception("WbiRetryTimesExceed");
        }
    }
}