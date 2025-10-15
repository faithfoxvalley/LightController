using LightController.Config.Bacnet;
using LightController.Config.Dmx;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LightController.Config;

public class ConfigFile : ConfigBase
{
    [YamlMember(Description = "Name of the MIDI device, or blank to pick the first one")]
    public string MidiDevice { get; set; }

    [YamlMember(Alias = "Pro", Description = "ProPresenter settings")]
    public ProPresenterConfig ProPresenter { get; set; } = new ProPresenterConfig();

    [YamlMember(Description = "DMX settings")]
    public DmxConfig Dmx { get; set; } = new DmxConfig();


    public ConfigFile() { }

    public static ConfigFile Load(string fileLocation)
    {
        return Load<ConfigFile>(fileLocation);
    }

}
