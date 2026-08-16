using System;
using System.Collections.Generic;
using DuckovVP.Console.Parser;

namespace DuckovVP.Console;

public class Parsers: IDisposable
{
    public List<IParser> parsers = new();

    public Parsers()
    {
        parsers.Add(new YoutubeParser());
        parsers.Add(new BiliParser());
        parsers.Add(new NeteaseParser());
        parsers.Add(new TwitchParser());
    }

    public void Dispose()
    {
        parsers.ForEach(v => v.Dispose());
        parsers.Clear();
    }
}