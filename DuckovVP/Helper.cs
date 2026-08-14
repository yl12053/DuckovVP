using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace DuckovVP;

public static class Helper
{
    public static UniTask<T> AsUniTaskCpy<T>(this Task<T> task, bool useCurrentSynchronizationContext = true)
    {
        UniTaskCompletionSource<T> completionSource1 = new UniTaskCompletionSource<T>();
        task.ContinueWith((x, state) =>
        {
            UniTaskCompletionSource<T> completionSource2 = (UniTaskCompletionSource<T>) state;
            switch (x.Status)
            {
                case TaskStatus.RanToCompletion:
                    completionSource2.TrySetResult(x.Result);
                    break;
                case TaskStatus.Canceled:
                    completionSource2.TrySetCanceled();
                    break;
                case TaskStatus.Faulted:
                    completionSource2.TrySetException(x.Exception?.InnerException ?? x.Exception);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }, completionSource1, TaskScheduler.Default);
        return completionSource1.Task;
    }

    public static async UniTask<string> ReadAsStringCompressAsync(this HttpContent content, CancellationToken token = default)
    {
        var encodings = content.Headers.ContentEncoding;
        if (encodings.Count == 0)
        {
            return await content.ReadAsStringAsync().AsUniTaskCpy().AttachExternalCancellation(token);
        }

        var stream = await content.ReadAsStreamAsync().AsUniTaskCpy().AttachExternalCancellation(token);
        foreach (var encoding in encodings.Reverse())
        {
            stream = encoding switch
            {
                "gzip" => new GZipStream(stream, CompressionMode.Decompress),
                "deflate" => new DeflateStream(stream, CompressionMode.Decompress),
                "br" => new BrotliStream(stream, CompressionMode.Decompress),
                _ => stream
            };
        }

        using var target = stream;
        using var reader = new StreamReader(target, Encoding.UTF8);
        return await reader.ReadToEndAsync().AsUniTaskCpy().AttachExternalCancellation(token);
    } 
    
    public static async UniTask<byte[]> ReadAsByteArrayCompressAsync(this HttpContent content, CancellationToken token = default)
    {
        var encodings = content.Headers.ContentEncoding;
        if (encodings.Count == 0)
        {
            return await content.ReadAsByteArrayAsync().AsUniTaskCpy().AttachExternalCancellation(token);
        }

        var stream = await content.ReadAsStreamAsync().AsUniTaskCpy().AttachExternalCancellation(token);
        foreach (var encoding in encodings.Reverse())
        {
            stream = encoding switch
            {
                "gzip" => new GZipStream(stream, CompressionMode.Decompress),
                "deflate" => new DeflateStream(stream, CompressionMode.Decompress),
                "br" => new BrotliStream(stream, CompressionMode.Decompress),
                _ => stream
            };
        }

        using var target = stream;
        using MemoryStream mem = new MemoryStream();
        await target.CopyToAsync(mem, token);
        return mem.ToArray();
    } 
}