using LightController.Config.Bacnet;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace LightController.Config;
public  class ShowConfig : ConfigBase
{

    [YamlMember(Description = "Scene to load on startup")]
    public string DefaultScene { get; set; }
    [YamlMember(Description = "List of lighting scenes")]
    public List<Scene> Scenes { get; set; } = new List<Scene>();

    [YamlMember(Description = "Transition time in seconds for switching scenes")]
    public double DefaultTransitionTime { get; set; } = 1;

    [YamlMember(Description = "BACnet settings")]
    public BacnetConfig Bacnet { get; set; } = new BacnetConfig();

    public static OpenFileDialog CreateOpenDialog()
    {
        return new OpenFileDialog()
        {
            Filter = "Light Show file (*.show)|*.show|YAML files (.yml)|*.yml;*.yaml",
            Multiselect = false,
        };
    }

    public static SaveFileDialog CreateSaveDialog()
    {
        return new SaveFileDialog()
        {
            Filter = "Light Show file (*.show)|*.show|Yaml file (*.yml)|*.yml;*.yaml"
        };
    }

    public static bool TryGetFilePathFromArgs(CommandLineOptions args, out string configFile)
    {
        if (args.TryGetFlaglessArg(0, out configFile) && IsValidExtension(configFile) && File.Exists(configFile))
            return true;
        return args.TryGetFlagArg("config", 0, out configFile) && IsValidExtension(configFile) && File.Exists(configFile);
    }

    public static bool IsValidExtension(string file)
    {
        return file.EndsWith(".yaml", StringComparison.InvariantCultureIgnoreCase)
            || file.EndsWith(".yml", StringComparison.InvariantCultureIgnoreCase)
            || file.EndsWith(".show", StringComparison.InvariantCultureIgnoreCase);
    }

    internal static ShowConfig Load(string fileLocation)
    {
        return ConfigBase.Load<ShowConfig>(fileLocation);
    }
}
