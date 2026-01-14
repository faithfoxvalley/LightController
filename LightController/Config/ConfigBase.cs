using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
    private string fileLocation;
    [YamlIgnore]
    private MemoryStream fileContents;

    public void Save(string fileLocation = null)
    {
        if (fileLocation == null)
            fileLocation = this.fileLocation;
        if(fileContents != null)
        {
            fileContents.Position = 0;
            using FileStream fs = File.Create(fileLocation);
            using GZipStream gzip = new GZipStream(fileContents, CompressionMode.Decompress, true);
            gzip.CopyTo(fs);
            return;
        }

        using (StreamWriter writer = File.CreateText(fileLocation))
        {
            SerializerBuilder serializer = new SerializerBuilder()
                .IgnoreFields()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)
                .WithNamingConvention(UnderscoredNamingConvention.Instance);
            foreach (var tag in GetYamlTags())
                serializer.WithTagMapping(tag.Item1, tag.Item2);

            serializer.Build().Serialize(writer, this);
        }
    }

    protected static T Load<T>(string fileLocation, bool immutable=true) where T : ConfigBase, new()
    {
        if (File.Exists(fileLocation))
        {
            T existingFile;
            using MemoryStream mem = new MemoryStream();
            using (FileStream fs = File.OpenRead(fileLocation))
                fs.CopyTo(mem);
            mem.Position = 0;
            using (StreamReader reader = new StreamReader(mem, leaveOpen: true))
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
                if(immutable)
                {
                    mem.Position = 0;
                    MemoryStream compressedContents = new MemoryStream();
                    using (GZipStream gzip = new GZipStream(compressedContents, CompressionLevel.SmallestSize, true))
                        mem.CopyTo(gzip);
                    existingFile.fileContents = compressedContents;
                }
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
