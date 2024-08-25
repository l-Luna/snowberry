using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Celeste;
using Celeste.Mod;
using Ionic.Zip;

namespace Snowberry.Editor;

public static class Backups{

    public enum BackupReason{
        OnOpen,
        OnSave,
        OnPlaytest,
        Autosave,
        OnClose,
        Unknown
    }

    public class Backup{
        public string Path;
        public AreaKey? For;
        public DateTime Timestamp;
        public BackupReason Reason;
        public long Filesize;
    }

    // for yaml serialization
    private class Meta{
        public string Timestamp;
        public string Reason;
    }

    public const string MetaFilename = "snowberrymeta.yaml";
    public const string MapFilename = "map.bin";

    /*
     * a backup is a zip file containing the map file itself, a meta file, and any other relevant files (e.g. eventually XMLs).
     * stored in %AppData%/snowberry/backups/<map path>/, or the OS equivalent.
     */

    public static List<Backup> GetBackupsFor(MapId id){
        string dir = BackupsDirectoryFor(id);
        if(Directory.Exists(dir)){
            List<Backup> ret = [];

            foreach (string file in Directory.EnumerateFiles(dir)){
                if (Path.GetExtension(file) == ".zip"){
                    using var zip = ZipFile.Read(file);
                    if(zip.ContainsEntry(MetaFilename) && zip.ContainsEntry(MapFilename)){
                        var metaEntry = zip[MetaFilename];
                        string data = metaEntry.AlternateEncoding.GetString(metaEntry.ExtractStream().ToArray());
                        Meta meta = YamlHelper.Deserializer.Deserialize<Meta>(data);
                        ret.Add(new Backup{
                            Path = file,
                            For = id.Key(),
                            Timestamp = DateTime.Parse(meta.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime(),
                            Reason = Enum.TryParse<BackupReason>(meta.Reason, out var r) ? r : BackupReason.Unknown,
                            Filesize = new FileInfo(file).Length
                        });
                    }
                }
            }

            return ret;
        }

        return [];
    }

    public static void SaveBackup(byte[] data, MapId id, BackupReason reason) {
        DateTime now = DateTime.Now.ToUniversalTime();

        var dir = BackupsDirectoryFor(id);
        Directory.CreateDirectory(dir);

        string meta = $"""
                       Timestamp: "{now:O}"
                       Reason: "{reason.ToString()}"
                       """;

        using ZipFile file = new ZipFile();
        file.AddEntry(MapFilename, data);
        file.AddEntry(MetaFilename, meta);
        file.Save(Path.Combine(dir, $"backup-{now:yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'-'fff}-{reason.ToString()}.zip"));
    }

    public static string BackupsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "snowberry", "backups");

    public static string BackupsDirectoryFor(MapId id) =>
        Path.Combine(BackupsDirectory, id.SID());
}