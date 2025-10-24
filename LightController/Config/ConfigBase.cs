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
public class ConfigBase
{
    [YamlIgnore]
    public string FileLocation => fileLocation;
    [YamlIgnore]
    protected string fileLocation;

    public void Save(string fileLocation = null)
    {
        if (fileLocation == null)
            fileLocation = this.fileLocation;
        using (StreamWriter writer = File.CreateText(fileLocation))
        {
            SerializerBuilder serializer = new SerializerBuilder()
                .IgnoreFields()
                .WithNamingConvention(UnderscoredNamingConvention.Instance);
            foreach (var tag in GetYamlTags())
                serializer.WithTagMapping(tag.Item1, tag.Item2);

            serializer.Build().Serialize(writer, this);
        }
    }

    protected static T Load<T>(string fileLocation) where T : ConfigBase, new()
    {
        if (File.Exists(fileLocation))
        {
            T existingFile;
            using (StreamReader reader = File.OpenText(fileLocation))
            {
                DeserializerBuilder deserializer = new DeserializerBuilder()
                    .IgnoreFields()
                    .IgnoreUnmatchedProperties()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance);
                foreach (var tag in GetYamlTags())
                    deserializer.WithTagMapping(tag.Item1, tag.Item2);

                existingFile = deserializer.Build().Deserialize<T>(reader);
            }
            if (existingFile != null)
            {
                existingFile.fileLocation = fileLocation;
                return existingFile;
            }
        }

        T newFile = new T();
        newFile.fileLocation = fileLocation;
        newFile.Save();
        return newFile;
    }

    protected static IEnumerable<Tuple<TagName, Type>> GetYamlTags()
    {
        foreach (Type t in typeof(MainWindow).Assembly.GetTypes())
        {
            if (t.IsDefined(typeof(YamlTagAttribute), false))
            {
                YamlTagAttribute attr = t.GetCustomAttribute<YamlTagAttribute>();
                yield return new Tuple<TagName, Type>(attr.Tag, t);
            }
        }
    }



    public void Open()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "code",
                Arguments = "\"" + fileLocation + "\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            Process.Start(startInfo);
        }
        catch (Win32Exception e)
        {
            if (e.NativeErrorCode != 0x00000002)
                throw;

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "explorer",
                Arguments = "/select,\"" + fileLocation + "\"",
            };

            Process.Start(startInfo);
        }
    }

}
