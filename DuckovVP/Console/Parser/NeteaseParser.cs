using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Random = System.Random;

namespace DuckovVP.Console.Parser;

public class NeteaseParser: IParser
{
    public static readonly BigInteger Modulus = BigInteger.Parse(
        "00e0b509f6259df8642dbc35662901477df22677ec152b5ff68ace615bb7b725152b3ab17a876aea8a5aa76d2e417629ec4ee341f56135fccf695280104e0312ecbda92557c93870114af6c9d05c4f7f0c3685b7a46bee255932575cce10b424d813cfe4875d3e82047b97ddef52741d546b8e289dc6935b3ece0462db0a22b8e7",
        NumberStyles.HexNumber
    );
    public static readonly BigInteger PubKey = BigInteger.Parse("010001", NumberStyles.HexNumber);
    public static readonly byte[] Nonce = "0CoJUm6Qyw8W8jud"u8.ToArray();
    
    public static string RsaEncrypt(byte[] text)
    {
        
        BigInteger baseNum = new BigInteger(text, isUnsigned: true, isBigEndian: false);
        
        BigInteger rs = BigInteger.ModPow(baseNum, PubKey, Modulus);
        
        return BigInteger.Abs(rs).ToString("x").TrimStart('0').PadLeft(256, '0');
    }

    public static string AesEncrypt(string text, byte[] secKey)
    {
        byte[] iv = "0102030405060708"u8.ToArray();
        byte[] plain = Encoding.UTF8.GetBytes(text);

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = secKey;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        return Convert.ToBase64String(encrypted);
    }

    public static (string, string) MakeRequest(string data)
    {
        var secKey = new byte[16];
        Random random = new Random();

        for (int i = 0; i < 16; i++)
        {
            secKey[i] = (byte)"0123456789abcdef"[random.Next(16)];
        }

        var fp = AesEncrypt(data, Nonce);
        var encText = AesEncrypt(fp, secKey);
        var encSecKey = RsaEncrypt(secKey);
        return (encText, encSecKey);
    }
    
    private HttpClient _httpClient;
    public NeteaseParser()
    {
        _httpClient = new();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
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

        return host.Equals("music.163.com");
    }

    public bool IsValid(string url)
    {
        if (!ShallIntercept(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (uri.AbsolutePath.Equals("/"))
        {
            uri = new Uri(new Uri("https://" + uri.Host), uri.Fragment.TrimStart('#'));
        }

        if (!uri.AbsolutePath.Equals("/song"))
        {
            return false;
        }
        
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        if (string.IsNullOrWhiteSpace(queryParams["id"])) return false;
        return ulong.TryParse(queryParams["id"], out var id) && id > 0;
    }

    public async UniTask<string[]> Parse(string url, CancellationToken token)
    {
        Uri uri = new(url, UriKind.Absolute);
        if (uri.AbsolutePath.Equals("/"))
        {
            uri = new Uri(new Uri("https://" + uri.Host), uri.Fragment.TrimStart('#'));
        }
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        
        try
        {
            var ipRequest = await _httpClient.GetAsync("https://nextmusic.toubiec.cn/api/ip", token);
            ipRequest.EnsureSuccessStatusCode();
            var contents = await ipRequest.Content.ReadAsStringCompressAsync(token);
            var cnts = JObject.Parse(contents);
            var codes = cnts["code"].Value<int>();
            if (!(codes >= 200 && codes <= 299)) throw new HttpRequestException("Code not valid when IP");
            var ip = cnts["data"]["ip"].Value<string>();
            JObject payload = new()
            {
                ["id"] = queryParams["id"],
                ["level"] = "standard",
                ["ip"] = ip
            };
            var payloadStr = payload.ToString(Newtonsoft.Json.Formatting.None);
            var content = new StringContent(payloadStr, Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync("https://nextmusic.toubiec.cn/api/getSongUrl", content, token);
            resp.EnsureSuccessStatusCode();
            var retstr = await resp.Content.ReadAsStringCompressAsync(token);
            var cnt = JObject.Parse(retstr);
            var code = cnt["code"].Value<int>();
            if (!(code >= 200 && code <= 299)) throw new HttpRequestException("Code not valid");
            var result = cnt?["data"]?["url"]?.Value<string>() ?? null;
            if (result == null) throw new HttpRequestException("Data parse failed");
            return new[] { result, "" };
        }
        catch (HttpRequestException exception)
        {
            Debug.LogException(exception);
            var headRequest = new HttpRequestMessage(HttpMethod.Head,
                $"https://music.163.com/song/media/outer/url?id={queryParams["id"]}.mp3");
            var headResult = await _httpClient.SendAsync(headRequest, token);
            var statusCode = headResult.StatusCode;
            if ((int)statusCode >= 300 && (int)statusCode <= 399)
            {
                if (headResult.Headers.Location.AbsolutePath.Equals("/404")) throw new Exception("Barrier");
                return new[] { headResult.Headers.Location.AbsoluteUri, "" };
            }
            if ((int)statusCode >= 200 && (int)statusCode <= 299)
            {
                return new[] { $"https://music.163.com/song/media/outer/url?id={queryParams["id"]}.mp3", "" };
            }

            throw new HttpRequestException("Not success");
        }
    }

    public async UniTask<string[]> Info(string url, CancellationToken token)
    {
        Uri uri = new(url, UriKind.Absolute);
        if (uri.AbsolutePath.Equals("/"))
        {
            uri = new Uri(new Uri("https://" + uri.Host), uri.Fragment.TrimStart('#'));
        }
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        var id = queryParams["id"];

        string name = "";
        string artist = "";
        string pic = "";
        
        try
        {
            var ipRequest = await _httpClient.GetAsync("https://nextmusic.toubiec.cn/api/ip", token);
            ipRequest.EnsureSuccessStatusCode();
            var contents = await ipRequest.Content.ReadAsStringCompressAsync(token);
            var cnts = JObject.Parse(contents);
            var codes = cnts["code"].Value<int>();
            if (!(codes >= 200 && codes <= 299)) throw new HttpRequestException("Code not valid when IP");
            var ip = cnts["data"]["ip"].Value<string>();
            JObject payload = new()
            {
                ["id"] = queryParams["id"],
                ["ip"] = ip
            };
            var payloadStr = payload.ToString(Formatting.None);
            var contentz = new StringContent(payloadStr, Encoding.UTF8, "application/json");
            var respz = await _httpClient.PostAsync("https://nextmusic.toubiec.cn/api/getSongInfo", contentz, token);
            respz.EnsureSuccessStatusCode();
            var retstr = await respz.Content.ReadAsStringCompressAsync(token);
            var cnt = JObject.Parse(retstr);
            var code = cnt["code"].Value<int>();
            if (!(code >= 200 && code <= 299)) throw new HttpRequestException("Code not valid");
            name = cnt?["data"]?["name"]?.Value<string>() ?? "";
            artist = cnt?["data"]?["singer"]?.Value<string>() ?? "";
            pic = cnt?["data"]?["picimg"]?.Value<string>() ?? "";
            if (!(string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(artist) ||
                  string.IsNullOrWhiteSpace(pic)))
            {
                return new[] { name, artist, pic };
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "https://music.163.com/weapi/v3/song/detail?csrf_token=");
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Accept-Encoding", "gzip,deflate");
        request.Headers.Add("Referer", "https://music.163.com/search/");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_9_2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/33.0.1750.152 Safari/537.36");
        request.Headers.Add("Cookie", "appver=1.5.2; os=linux");

        JObject requestData = new()
        {
            ["id"] = id,
            ["c"] = new JArray(new JObject { ["id"] = id }).ToString(Formatting.None),
            ["csrf_token"] = ""
        };
        (var param, var encSecKey) = MakeRequest(requestData.ToString(Formatting.None));
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"params", param},
            {"encSecKey", encSecKey}
        });
        request.Content = content;

        var response = await _httpClient.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
        var respText = await response.Content.ReadAsStringCompressAsync(token);
        var resp = JObject.Parse(respText);
        if (string.IsNullOrWhiteSpace(name)) name = resp?["songs"]?[0]?["name"]?.Value<string>() ?? "Unnamed CD";
        if (string.IsNullOrWhiteSpace(artist)) artist = resp?["songs"]?[0]?["ar"]?[0]?["name"]?.Value<string>() ?? "Unknown Artist";
        if (string.IsNullOrWhiteSpace(pic)) pic = resp?["songs"]?[0]?["al"]?["picUrl"]?.Value<string>() ?? "";
        return new[] { name, artist, pic };
    }
}