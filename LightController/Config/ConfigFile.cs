using LightController.Config.Input;
using LightController.Config.Output;
using System;
using System.Collections.Generic;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LightController.Config
{
    public class ConfigFile
    {
        public List<InputBase> Inputs { get; set; } = new List<InputBase>();
        public List<OutputBase> Outputs { get; set; } = new List<OutputBase>();

        public ConfigFile() { }

        public static ConfigFile Load(string yml = null)
        {
            if(yml != null)
            {
                DeserializerBuilder deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance);
                foreach (var tag in GetYamlTags())
                    deserializer.WithTagMapping(tag.Item1, tag.Item2);

                return deserializer.Build().Deserialize<ConfigFile>(yml);
            }
            return new ConfigFile();
        }

        public void Save()
        {
            SerializerBuilder serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance);
            foreach (var tag in GetYamlTags())
                serializer.WithTagMapping(tag.Item1, tag.Item2);

            string yaml = serializer.Build().Serialize(this);
            System.Windows.Clipboard.SetText(yaml);
            ConfigFile test = ConfigFile.Load(yaml);
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
