using Celeste.Mod;
using Snowberry.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Snowberry.Editor.Placements;

namespace Snowberry;

public class PluginInfo {
    internal static readonly Dictionary<string, PluginInfo> Entities = new();
    internal static readonly Dictionary<string, PluginInfo> Stylegrounds = new();
    internal static readonly Dictionary<string, PluginInfo> OtherPlugins = new();

    protected readonly string name;
    private readonly ConstructorInfo ctor;

    public readonly Dictionary<string, PluginOption> Options;

    public readonly SnowberryModule Module;

    public PluginInfo(string name, Type t, ConstructorInfo ctor, SnowberryModule module) {
        this.name = name;
        this.ctor = ctor;
        Module = module;

        Dictionary<string, PluginOption> options = new();
        foreach (FieldInfo f in t.GetFields()) {
            if (f.GetCustomAttribute<OptionAttribute>() is OptionAttribute option) {
                if (string.IsNullOrEmpty(option.Name)) {
                    Snowberry.Log(LogLevel.Warn, $"'{f.Name}' ({f.FieldType.Name}) from plugin '{name}' was ignored because it had a null or empty option name!");
                } else if (!options.ContainsKey(option.Name))
                    options.Add(option.Name, new FieldOption(f, option.Name));
            }
        }

        Options = new Dictionary<string, PluginOption>(options);
    }

    public virtual T Instantiate<T>() where T : Plugin {
        T plugin = (T)ctor.Invoke(Array.Empty<object>());
        plugin.Info = this;
        plugin.Name = name;
        return plugin;
    }

    public static void GenerateFromAssembly(Assembly assembly, SnowberryModule module) {
        foreach (Type t in assembly.GetTypesSafe().Where(t => !t.IsAbstract && typeof(Plugin).IsAssignableFrom(t))) {
            bool isEntity = typeof(Entity).IsAssignableFrom(t);
            bool isStyleground = typeof(Styleground).IsAssignableFrom(t);

            foreach (PluginAttribute pl in t.GetCustomAttributes<PluginAttribute>(inherit: false)) {
                if (string.IsNullOrEmpty(pl.Name)) {
                    Snowberry.Log(LogLevel.Warn, $"Found plugin with null or empty name! skipping... (Type: {t})");
                    continue;
                }

                ConstructorInfo ctor = t.GetConstructor(Array.Empty<Type>());
                if (ctor == null) {
                    Snowberry.Log(LogLevel.Warn, $"'{pl.Name}' does not have a parameterless constructor, skipping...");
                    continue;
                }


                PluginInfo info = new PluginInfo(pl.Name, t, ctor, module);

                if (isEntity)
                    Entities.Add(pl.Name, info);
                else if (isStyleground)
                    Stylegrounds.Add(pl.Name, info);
                else
                    OtherPlugins.Add(pl.Name, info);

                Snowberry.Log(LogLevel.Verbose, $"Successfully registered '{pl.Name}' plugin");
            }

            if (isEntity) {
                MethodInfo addPlacements = t.GetMethod("AddPlacements");
                if (addPlacements != null) {
                    if (addPlacements.GetParameters().Length == 0)
                        addPlacements.Invoke(null, Array.Empty<object>());
                    else
                        Snowberry.Log(LogLevel.Warn, $"Found entity plugin with invalid AddPlacements (has parameters)! skipping... (Type: {t})");
                } else
                    Snowberry.Log(LogLevel.Verbose, $"Found entity plugin without placements. (Type: {t})");
            }
        }

        Snowberry.LogInfo($"Successfully registered {Entities.Count + Stylegrounds.Count + OtherPlugins.Count} plugins, and {EntityPlacementProvider.All.Count} placements");
    }
}

public interface DefaultedPluginInfo {
    bool TryGetDefault(string key, out object value);
}

public interface PluginOption {
    object GetValue(Plugin from);
    void SetValue(Plugin on, object value);
    Type FieldType { get; }
    string Key { get; }
    string Tooltip { get; }
}

public class FieldOption(FieldInfo field, string key) : PluginOption {

    public object GetValue(Plugin from) => field.GetValue(from);

    public void SetValue(Plugin on, object value) => field.SetValue(on, value);

    public Type FieldType => field.FieldType;

    public string Key { get; } = key;

    public string Tooltip => null;
}

public class UnknownPluginInfo(string name) : PluginInfo(name, typeof(Plugin), null, CelesteEverest.INSTANCE);