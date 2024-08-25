using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste;
using Celeste.Mod;

namespace Snowberry;

public static class Files {

    // assume the worst (windows)
    // see https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file
    public static readonly HashSet<string> IllegalFilenames = [
        "CON", "PRN", "AUX", "NUL", "COM", "LPT"
    ];
    public static readonly List<string> IllegalFilenamesSuffixed = [
        "COM", "LPT"
    ];
    public static readonly List<char> IllegalFilenameSuffixes = [
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '\u00b9', '\u00b2', '\u00b3'
    ];
    public static readonly HashSet<char> IllegalFilenameChars = [
        '/', '\\', ':', '<', '>', '"', '|', '?', '*', /* we choose the extension */ '.'
    ];

    // adapted from https://stackoverflow.com/a/4975942
    public static string FormatFilesize(long bytes){
        string[] suffixes = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
        if(bytes == 0)
            return "0" + suffixes[0];
        long absBytes = Math.Abs(bytes);
        int place = Convert.ToInt32(Math.Floor(Math.Log(absBytes, 1024)));
        double num = Math.Round(absBytes / Math.Pow(1024, place), 1);
        return Math.Sign(bytes) * num + suffixes[place];
    }

    // adapted from https://stackoverflow.com/a/468131
    public static long DirSize(DirectoryInfo d) =>
        d.GetFiles().Sum(fi => fi.Length) + d.GetDirectories().Sum(DirSize);

    // from FileProxy
    public static string Modize(string path){
        string directoryName = Path.GetDirectoryName(path);
        path = Path.GetFileNameWithoutExtension(path);
        if(!string.IsNullOrEmpty(directoryName))
            path = Path.Combine(directoryName, path);
        if(path.StartsWith(Everest.Content.PathContentOrig))
            path = path[(Everest.Content.PathContentOrig.Length + 1)..];
        path = path.Replace('\\', '/');
        return path;
    }

    public static string GetRealPath(string path){
        Everest.Content.TryGet(Modize(path), out ModAsset asset);
        return asset switch{
            FileSystemModAsset fs => fs.Path,
            MapBinsInModsModAsset map => map.Path,
            _ => null
        };
    }

    public static string KeyToPath(AreaKey key) =>
        GetRealPath(Path.Combine("Maps", AreaData.Get(key).Mode[(int)key.Mode].Path + ".bin"));

    public static bool IsLooseMap(AreaKey key){
        string qpath = Path.Combine("Maps", AreaData.Get(key).Mode[(int)key.Mode].Path + ".bin");
        Everest.Content.TryGet(Modize(qpath), out ModAsset asset);
        return asset is MapBinsInModsModAsset;
    }

    public static bool IsValidFilename(string filename){
        if(filename.Length == 0)
            return false;

        if(IllegalFilenames.Contains(filename))
            return false;

        foreach(string name in IllegalFilenamesSuffixed)
            foreach(char suffix in IllegalFilenameSuffixes)
                if(filename == name + suffix)
                    return false;

        if(IllegalFilenameChars.Any(filename.Contains))
            return false;

        return true;
    }

    public static string CleanPath(string path){
        foreach(char c in IllegalFilenameChars){
            if(c == '/')
                continue;
            path = path.Replace(c, c == '\\' ? '/' : '_');
        }

        return path;
    }
}