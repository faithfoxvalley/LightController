using LightController.Config.Dmx;
using LightController.Config.Input;
using LightController.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LightController.Config
{
    public class ConfigFile
    {
        private const string FileName = "config.yml";

        public string MidiDevice { get; set; }
        public string DefaultScene { get; set; }

        [YamlMember(Alias = "Pro")]
        public ProPresenterConfig ProPresenter { get; set; }

        public DmxConfig Dmx { get; set; }

        public List<Scene> Scenes { get; set; } = new List<Scene>();

        public ConfigFile() { }


        public static string GetUserFileLocation()
        {
            return Path.Combine(MainWindow.Instance.ApplicationData, FileName);
        }

        public static ConfigFile Load()
        {
            string file = GetUserFileLocation();
            if (File.Exists(file))
            {
                ConfigFile existingFile;
                using (StreamReader reader = File.OpenText(file))
                {
                    DeserializerBuilder deserializer = new DeserializerBuilder()
                        .IgnoreFields()
                        .IgnoreUnmatchedProperties()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance);
                    foreach (var tag in GetYamlTags())
                        deserializer.WithTagMapping(tag.Item1, tag.Item2);

                    existingFile = deserializer.Build().Deserialize<ConfigFile>(reader);
                }
                existingFile.Save();
                return existingFile;
            }
            ConfigFile newFile = new ConfigFile();
            newFile.Save();
            return newFile;
        }

        public void Save()
        {
            using(StreamWriter writer = File.CreateText(GetUserFileLocation()))
            {
                SerializerBuilder serializer = new SerializerBuilder()
                    .IgnoreFields()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance);
                foreach (var tag in GetYamlTags())
                    serializer.WithTagMapping(tag.Item1, tag.Item2);

                serializer.Build().Serialize(writer, this);
            }
        }

        private static IEnumerable<Tuple<TagName, Type>> GetYamlTags()
        {
            foreach(Type t in typeof(MainWindow).Assembly.GetTypes())
            {
                if(t.IsDefined(typeof(YamlTagAttribute), false))
                {
                    YamlTagAttribute attr = t.GetCustomAttribute<YamlTagAttribute>();
                    yield return new Tuple<TagName, Type>(attr.Tag, t);
                }
            }
        }
    }
}
