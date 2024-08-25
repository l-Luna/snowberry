using System;
using System.Linq;
using Celeste;
using Celeste.Mod;
using JetBrains.Annotations;

namespace Snowberry;

public interface MapId {

    // 4 cases: loaded map, unloaded map, unsaved map, edit of a locked (vanilla/zipped) map
    // locked maps are treated like unsaved maps, except with better visuals and hints

    // display name
    public string Name();

    // identitifer used to uniquely refer to this, e.g. for backups
    public string SID();

    // actual path on disk, if it exists
    [CanBeNull]
    public string Path();

    // key for the "real" version of this map
    public AreaKey? Key();
}

public record LoadedMapId(AreaKey key) : MapId {

    public string Name() => key.SID;

    public string SID() => key.SID;

    public string Path() => Files.KeyToPath(key);

    public AreaKey? Key() => key;
}

public record LooseMapId(string path) : MapId {

    public string Name() => System.IO.Path.GetFileName(path);

    public string SID() {
        string cleaned = path;
        // remove .bin
        if (cleaned.EndsWith(".bin", StringComparison.Ordinal))
            cleaned = cleaned[..".bin".Length];
        // if this is relative to the Mods folder, use its path from there as the SID
        if (cleaned.StartsWith(Everest.Loader.PathMods, StringComparison.Ordinal))
            return cleaned[Everest.Loader.PathMods.Length..].TrimStart('/', '\\');
        // otherwise, just try to clean up the whole name
        return Files.CleanPath(cleaned);
    }

    public string Path() => path;

    public AreaKey? Key() => null;
}

public record NewMapId(DateTime started) : MapId {

    public string Name() => Dialog.Clean("SNOWBERRY_EDITOR_NEW_MAP");

    public string SID() => "unsaved-" + started.Ticks;

    public string Path() => null;

    public AreaKey? Key() => null;
}

public record LockedMapId(AreaKey from, DateTime started) : MapId {

    public string Name() => from.SID;

    public string SID() => from.SID.Split('/').Last() + "-copy-" + started.Ticks;

    public string Path() => null;

    public AreaKey? Key() => from;
}