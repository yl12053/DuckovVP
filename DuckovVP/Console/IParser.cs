using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LibVLCSharp.Shared;

namespace DuckovVP.Console;

public interface IParser: IDisposable
{
    public bool ShallIntercept(string url);
    public bool IsValid(string url);
    public UniTask<string[]> Parse(string url, CancellationToken token);
    public UniTask<string[]> Info(string url, CancellationToken token);

    public void OnMediaCreate(Media media, string[] urls, string original)
    {
    }
}